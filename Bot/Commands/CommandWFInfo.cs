using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Bot.Extensions;
using CommandLine;
using Discord;
using WarframeNET;

namespace Bot.Commands {
    public class CommandWFInfo : Command {
        public static string TrackedPlatform => Platform.PC;
        private const string ConfigName = "tracked";

        private bool _day;
        private readonly HashSet<string> _trackedIDs = new HashSet<string>();
        private readonly WarframeClient _client = new WarframeClient();
        private int _secondsToUpdate;

        public CommandWFInfo(string name) : base(name) {
            this.AddVerb<StateVerb>();
            this.AddVerb<AlertsVerb>();
            this.AddVerb<InvasionsVerb>();
            this.WithDescription("Tracks Warframe's state in the channel");
        }

        public override async Task Load() {
            // Prevent it from posting alerts as soon as the bot is reset
            WorldState state = await this._client.GetWorldStateAsync(CommandWFInfo.TrackedPlatform);
            foreach (Alert alert in state.WS_Alerts)
                this._trackedIDs.Add(alert.Id);
            foreach (Invasion invasion in state.WS_Invasions)
                this._trackedIDs.Add(invasion.Id);

            this._day = CommandWFInfo.IsDay(DateTime.UtcNow);

            // Start tracking
            Bot.Instance.SecondsTimer.Elapsed += this.Update;
        }

        public override Task Unload() {
            // Stop tracking
            Bot.Instance.SecondsTimer.Elapsed -= this.Update;

            return base.Unload();
        }

        private async void Update(object sender, ElapsedEventArgs elapsedEventArgs) {
            // Check if it's time to update
            if (this._secondsToUpdate-- > 0)
                return;
            this._secondsToUpdate = 10;

            WorldState state;
            try {
                state = await this._client.GetWorldStateAsync(CommandWFInfo.TrackedPlatform);
            } catch {
                return;
            }

            List<Task> tasks = new List<Task>();

            ConfigHandler.ConfigWrapper<Storage> config = this.GetConfig<Storage>(CommandWFInfo.ConfigName);
            IMessageChannel[] stateChannels = config.GetValue(c => c.StateChannels.ToArray())
                .Select(id => Bot.Instance.GetChannel(id))
                .Where(channel => channel is IMessageChannel)
                .Cast<IMessageChannel>()
                .ToArray();

            IMessageChannel[] alertChannels = config.GetValue(c => c.AlertChannels.ToArray())
                .Select(id => Bot.Instance.GetChannel(id))
                .Where(channel => channel is IMessageChannel)
                .Cast<IMessageChannel>()
                .ToArray();

            IMessageChannel[] invasionChannels = config.GetValue(c => c.InvasionChannels.ToArray())
                .Select(id => Bot.Instance.GetChannel(id))
                .Where(channel => channel is IMessageChannel)
                .Cast<IMessageChannel>()
                .ToArray();

            // Cetus Day/Night
            DateTime now = DateTime.UtcNow;
            bool day = CommandWFInfo.IsDay(now);
            if (day != this._day) {
                this._day = day;

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle($"{(day ? ":sunny:" : ":full_moon:")} Cetus");
                embed.WithDescription($"{CommandWFInfo.CycleTimeLeft(now).Format()} until {(day ? "night" : "day")}");
                embed.WithTimestamp(now - CommandWFInfo.CycleTime(now));

                tasks.Add(stateChannels.SendToAll("", embed: embed.Build()));
            }

            // Alerts
            foreach (Alert alert in state.WS_Alerts) {
                if (!this._trackedIDs.Add(alert.Id))
                    continue;
                this._trackedIDs.Add(alert.Id);

                // Only do important alerts
                if (!alert.Mission.Reward.IsImportant())
                    continue;

                StringBuilder description = new StringBuilder();
                if (alert.Mission.IsNightmare)
                    description.AppendLine("**NIGHTMARE** (No Shields)");
                if (alert.Mission.IsArchwingRequired)
                    description.AppendLine("Archwing Required");
                description.AppendLine($"{alert.Mission.Type} - {alert.Mission.Faction} ({alert.Mission.EnemyMinLevel}-{alert.Mission.EnemyMaxLevel})");
                description.AppendLine(string.Join(", ", alert.Mission.Reward.ImportantRewardStrings()));

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle($"Alert - {alert.Mission.Node} - {(alert.EndTime - alert.StartTime).Format()}");
                embed.WithDescription(description.ToString());
                embed.WithFooter((alert.EndTime - alert.StartTime).Format());
                embed.WithTimestamp(alert.StartTime);

                tasks.Add(alertChannels.SendToAll("@here", embed: embed.Build()));
            }

            // Invasions
            foreach (Invasion invasion in state.WS_Invasions) {
                if (!this._trackedIDs.Add(invasion.Id))
                    continue;
                this._trackedIDs.Add(invasion.Id);

                // Only do invasions in progress
                if (Math.Abs(invasion.Completion) > 100)
                    continue;

                // Only do important invasions
                if (!invasion.AttackerReward.IsImportant() && !invasion.DefenderReward.IsImportant())
                    continue;

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle($"Invasion - {invasion.Node} - {invasion.DefendingFaction} vs. {invasion.AttackingFaction}");
                embed.WithTimestamp(invasion.StartTime);
                embed.WithDescription($"*{invasion.Description}*");

                // Rewards
                embed.AddField(invasion.DefendingFaction, string.Join("\n", invasion.DefenderReward.ImportantRewardStrings()));
                if (!string.Equals(invasion.AttackingFaction, "infested", StringComparison.OrdinalIgnoreCase))
                    embed.AddField(invasion.AttackingFaction, string.Join("\n", invasion.AttackerReward.ImportantRewardStrings()));

                tasks.Add(invasionChannels.SendToAll("@here", embed: embed.Build()));
            }

            // Do tasks
            await Task.WhenAll(tasks);
        }

        #region Verbs
        [Verb("state", HelpText = "Displays Warframe world state information")]
        public class StateVerb : Verb {
            public override Task Execute(Command cmd, IMessage message, string[] args) {
                Task task = Task.CompletedTask;
                cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(config => {
                    if (config.StateChannels.Add(message.Channel.Id)) {
                        task = message.Reply("World state will now be tracked in this channel.");
                    } else if (config.StateChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("World state will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });
                Bot.Instance.Save();
                return task;
            }
        }

        [Verb("alerts", HelpText = "Displays Warframe alerts")]
        public class AlertsVerb : Verb {
            public override Task Execute(Command cmd, IMessage message, string[] args) {
                Task task = Task.CompletedTask;
                cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(config => {
                    if (config.AlertChannels.Add(message.Channel.Id)) {
                        task = message.Reply("Alerts will now be tracked in this channel.");
                    } else if (config.AlertChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("Alerts will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });
                Bot.Instance.Save();
                return task;
            }
        }

        [Verb("invasions", HelpText = "Displays Warframe invasions")]
        public class InvasionsVerb : Verb {
            public override Task Execute(Command cmd, IMessage message, string[] args) {
                Task task = Task.CompletedTask;
                cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(config => {
                    if (config.InvasionChannels.Add(message.Channel.Id)) {
                        task = message.Reply("Invasions will now be tracked in this channel.");
                    } else if (config.InvasionChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("Invasions will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });
                Bot.Instance.Save();
                return task;
            }
        }
        #endregion

        public class Storage : IConfig {
            public HashSet<ulong> StateChannels { get; set; } = new HashSet<ulong>();
            public HashSet<ulong> AlertChannels { get; set; } = new HashSet<ulong>();
            public HashSet<ulong> InvasionChannels { get; set; } = new HashSet<ulong>();
        }

        #region Static
        public static DateTime WFEpoch { get; } = new DateTime(2017, 11, 15, 20, 33, 0, DateTimeKind.Utc);
        public static TimeSpan DayLength { get; } = new TimeSpan(0, 100, 0);
        public static TimeSpan NightLength { get; } = new TimeSpan(0, 50, 0);
        public static TimeSpan CycleLength { get; } = CommandWFInfo.DayLength + CommandWFInfo.NightLength;

        private static bool IsDay(DateTime time) => CommandWFInfo.DayTime(time) < CommandWFInfo.DayLength;
        private static TimeSpan DayTime(DateTime time) => TimeSpan.FromTicks((time - CommandWFInfo.WFEpoch).Ticks % CommandWFInfo.CycleLength.Ticks);
        private static TimeSpan CycleTime(DateTime time) => CommandWFInfo.IsDay(time) ? CommandWFInfo.DayTime(time) : CommandWFInfo.DayTime(time) - CommandWFInfo.DayLength;
        private static TimeSpan CycleTimeLeft(DateTime time) => (CommandWFInfo.IsDay(time) ? CommandWFInfo.DayLength : CommandWFInfo.CycleLength) - CommandWFInfo.DayTime(time);
        #endregion
    }
}
