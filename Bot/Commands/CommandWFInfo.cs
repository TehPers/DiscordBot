using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Bot.Extensions;
using Bot.Helpers;
using CommandLine;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using WarframeNET;

namespace Bot.Commands {
    public class CommandWFInfo : Command {
        public static string TrackedPlatform { get; } = Platform.PC;
        private const string ConfigName = "tracked";

        private static readonly TimeSpan HistoryLength = new TimeSpan(days: 3, hours: 0, minutes: 0, seconds: 0);
        private static readonly Color ActiveColor = Color.DarkGreen;
        private static readonly Color ExpiredColor = Color.Red;

        private const int CetusUpdateRate = 60;
        private const int AlertsUpdateRate = 60;
        private const int InvasionsUpdateRate = 60;

        private static readonly TimeSpan WorldStateRate = new TimeSpan(hours: 0, minutes: 1, seconds: 0);
        private DateTimeOffset _lastWorldState = DateTimeOffset.Now - CommandWFInfo.WorldStateRate;
        private WorldState _worldState;
        private readonly SemaphoreSlim _worldStateLock = new SemaphoreSlim(1, 1);

        private bool _day;
        private readonly HashSet<string> _trackedIDs = new HashSet<string>();
        private readonly WarframeClient _client = new WarframeClient();
        private uint _secondsElapsed;

        public CommandWFInfo(string name) : base(name) {
            this.WithDescription("Tracks Warframe's state in the channel");
            this.AddVerb<CetusVerb>();
            this.AddVerb<AlertsVerb>();
            this.AddVerb<InvasionsVerb>();
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
            await this.UpdateMessages();
        }

        private Task UpdateMessages() {
            this._secondsElapsed++;

            List<Task> tasks = new List<Task>();
            if (this._secondsElapsed % CommandWFInfo.CetusUpdateRate == 0)
                tasks.Add(this.UpdateCetus());
            if (this._secondsElapsed % CommandWFInfo.AlertsUpdateRate == 0)
                tasks.Add(this.UpdateAlerts());
            if (this._secondsElapsed % CommandWFInfo.InvasionsUpdateRate == 0)
                tasks.Add(this.UpdateInvasions());

            return Task.WhenAll(tasks);
        }

        private async Task UpdateCetus() {
            ConfigHandler.ConfigWrapper<Storage> config = this.GetConfig<Storage>(CommandWFInfo.ConfigName);

            // Cetus Day/Night
            DateTime now = DateTime.UtcNow;
            bool day = CommandWFInfo.IsDay(now);
            if (day == this._day) {
                // Update messages
                TimedMessageInfo[] messages = config.GetValue(c => c.CetusMessages.ToArray());
                foreach (TimedMessageInfo messageInfo in messages) {
                    if (await messageInfo.GetMessage() is IUserMessage msg) {
                        await msg.ModifyAsync(m => m.Embed = CommandWFInfo.GetCetusEmbed().Build());
                    } else {
                        config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                    }
                }
            } else {
                this._day = day;

                // Delete current Cetus messages
                TimedMessageInfo[] messages = config.GetValue(c => c.CetusMessages.ToArray());
                foreach (TimedMessageInfo messageInfo in messages) {
                    if (await messageInfo.GetMessage() is IUserMessage msg)
                        await msg.DeleteAsync();

                    // Stop tracking the deleted message
                    config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                }

                // Get all tracked channels
                Dictionary<ulong, string> channelMessages = config.GetValue(c => c.CetusChannels.ToDictionary(kv => kv.Key, kv => kv.Value));
                IMessageChannel[] channels = channelMessages.Keys
                    .Select(id => Bot.Instance.GetChannel(id))
                    .Where(channel => channel is IMessageChannel)
                    .Cast<IMessageChannel>()
                    .ToArray();

                // Create new messages
                Embed embed = CommandWFInfo.GetCetusEmbed().Build();
                await Task.WhenAll(channels.Select(channel => {
                    // Send a new message
                    return channel.SendMessageAsync(channelMessages[channel.Id], embed: embed)

                    // Track it
                    .ContinueWith(task => {
                        config.SetValue(c => c.CetusMessages.Add(new TimedMessageInfo {
                            MessageID = task.Result.Id,
                            ChannelID = task.Result.Channel.Id,
                            DeleteTime = now + CommandWFInfo.CycleTimeLeft(now)
                        }));
                    });
                }));
            }

            await config.Save();
        }

        private async Task UpdateAlerts() {
            ConfigHandler.ConfigWrapper<Storage> config = this.GetConfig<Storage>(CommandWFInfo.ConfigName);

            // Update messages
            TimedMessageInfo[] messages = config.GetValue(c => c.AlertMessages.ToArray());
            foreach (TimedMessageInfo messageInfo in messages) {
                if (messageInfo.ShouldDelete) {
                    // Make sure to modify the config properly
                    config.SetValue(c => c.AlertMessages.Remove(messageInfo));

                    // Delete the message
                    if (await messageInfo.GetMessage() is IUserMessage msg) {
                        await msg.DeleteAsync();
                    } else {
                        config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                    }
                } else if (messageInfo.ShouldExpire) {
                    // Make sure to modify the config properly
                    config.SetValue(c => messageInfo.Expired = true);

                    // Modify the message
                    if (await messageInfo.GetMessage() is IUserMessage msg) {
                        await msg.ModifyAsync(m => m.Embed = msg.Embeds.FirstOrDefault()?.AsBuilder().WithColor(CommandWFInfo.ExpiredColor).Build());
                    } else {
                        config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                    }
                }
            }

            // Get tracked channels
            Dictionary<ulong, string> channelMessages = config.GetValue(c => c.AlertChannels.ToDictionary(kv => kv.Key, kv => kv.Value));
            IMessageChannel[] channels = channelMessages.Keys
                .Select(id => Bot.Instance.GetChannel(id))
                .Where(channel => channel is IMessageChannel)
                .Cast<IMessageChannel>()
                .ToArray();

            // Post new alerts
            WorldState state = await this.GetWorldState();
            foreach (Alert alert in state.WS_Alerts) {
                // Make sure it isn't being tracked, and track it if necessary
                if (!this._trackedIDs.Add(alert.Id))
                    continue;

                // Only do important alerts
                if (!alert.Mission.Reward.IsImportant())
                    continue;

                // Get the embed
                EmbedBuilder embed = CommandWFInfo.GetAlertEmbed(alert);

                // Create new messages
                await Task.WhenAll(channels.Select(channel => {
                    // Send a new message
                    return channel.SendMessageAsync(channelMessages[channel.Id], embed: embed)
                    // Track it
                    .ContinueWith(task => {
                        config.SetValue(c => c.AlertMessages.Add(new TimedMessageInfo {
                            MessageID = task.Result.Id,
                            ChannelID = task.Result.Channel.Id,
                            ExpireTime = alert.EndTime,
                            DeleteTime = alert.EndTime + CommandWFInfo.HistoryLength
                        }));
                    });
                }));
            }

            await config.Save();
        }

        private async Task UpdateInvasions() {
            ConfigHandler.ConfigWrapper<Storage> config = this.GetConfig<Storage>(CommandWFInfo.ConfigName);
            WorldState state = await this.GetWorldState();

            // Update messages
            InvasionMessageInfo[] messages = config.GetValue(c => c.InvasionMessages.ToArray());
            foreach (InvasionMessageInfo messageInfo in messages) {
                if (messageInfo.ShouldDelete) {
                    // Make sure to modify the config properly
                    config.SetValue(c => c.InvasionMessages.Remove(messageInfo));

                    // Delete the message
                    if (await messageInfo.GetMessage() is IUserMessage msg) {
                        await msg.DeleteAsync();
                    } else {
                        config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                    }
                } else if (!messageInfo.Expired) {
                    // Get the invasion
                    Invasion invasion = state.WS_Invasions.FirstOrDefault(i => i.Id == messageInfo.InvasionID);

                    // Check if it's expired
                    if (invasion == null || Math.Abs(invasion.Completion) > 100) {
                        // Make sure to modify the config properly
                        config.SetValue(c => messageInfo.Expired = true);

                        // Modify the message
                        if (await messageInfo.GetMessage() is IUserMessage msg) {
                            await msg.ModifyAsync(m => m.Embed = msg.Embeds.FirstOrDefault()?.AsBuilder().WithColor(CommandWFInfo.ExpiredColor).Build());
                        } else {
                            config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                        }
                    }
                }
            }

            // Get tracked channels
            Dictionary<ulong, string> channelMessages = config.GetValue(c => c.InvasionChannels.ToDictionary(kv => kv.Key, kv => kv.Value));
            IMessageChannel[] channels = channelMessages.Keys
                .Select(id => Bot.Instance.GetChannel(id))
                .Where(channel => channel is IMessageChannel)
                .Cast<IMessageChannel>()
                .ToArray();

            // Post new invasions
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

                // Get the embed
                EmbedBuilder embed = CommandWFInfo.GetInvasionEmbed(invasion);

                // Create new messages
                await Task.WhenAll(channels.Select(channel => {
                    // Send a new message
                    return channel.SendMessageAsync(channelMessages[channel.Id], embed: embed)
                    // Track it
                    .ContinueWith(task => {
                        config.SetValue(c => c.InvasionMessages.Add(new InvasionMessageInfo {
                            MessageID = task.Result.Id,
                            ChannelID = task.Result.Channel.Id,
                            InvasionID = invasion.Id
                        }));
                    });
                }));
            }

            await config.Save();
        }

        private async Task<WorldState> GetWorldState() {
            // Make sure this code only happens once
            if (!await this._worldStateLock.WaitAsync(5000))
                throw new Exception("Timed out");

            // Update the world state if necessary
            if (DateTimeOffset.Now - this._lastWorldState >= CommandWFInfo.WorldStateRate) {
                try {
                    this._lastWorldState = DateTimeOffset.Now;
                    this._worldState = await this._client.GetWorldStateAsync(CommandWFInfo.TrackedPlatform);
                } catch (Exception ex) {
                    Bot.Instance.Log("Failed to download Warframe world state", LogSeverity.Warning, exception: ex);
                }
            }

            this._worldStateLock.Release();
            return this._worldState;
        }

        private static EmbedBuilder GetCetusEmbed() => CommandWFInfo.GetCetusEmbed(DateTime.UtcNow);
        private static EmbedBuilder GetCetusEmbed(DateTime time) {
            bool day = CommandWFInfo.IsDay(time);

            EmbedBuilder embed = new EmbedBuilder {
                Title = $"{(day ? ":sunny:" : ":full_moon:")} Cetus",
                Description = $"{(day ? "Day" : "Night")} time remaining: {CommandWFInfo.CycleTimeLeft(time).Format()}",
                Color = CommandWFInfo.ActiveColor,
                Timestamp = time - CommandWFInfo.CycleTime(time)
            };
            return embed;
        }

        private static EmbedBuilder GetAlertEmbed(Alert alert) {
            StringBuilder description = new StringBuilder();
            if (alert.Mission.IsNightmare)
                description.AppendLine("**NIGHTMARE** (No Shields)");
            if (alert.Mission.IsArchwingRequired)
                description.AppendLine("Archwing Required");
            description.AppendLine($"{alert.Mission.Type} - {alert.Mission.Faction} ({alert.Mission.EnemyMinLevel}-{alert.Mission.EnemyMaxLevel})");
            description.AppendLine(string.Join(", ", alert.Mission.Reward.ImportantRewardStrings()));

            return new EmbedBuilder {
                Title = $"Alert - {alert.Mission.Node} - {(alert.EndTime - alert.StartTime).Format()}",
                Description = description.ToString(),
                Color = CommandWFInfo.ActiveColor,
                Timestamp = alert.StartTime,
                Footer = new EmbedFooterBuilder {
                    Text = (alert.EndTime - alert.StartTime).Format()
                }
            };
        }

        private static EmbedBuilder GetInvasionEmbed(Invasion invasion) {
            EmbedBuilder embed = new EmbedBuilder {
                Title = $"Invasion - {invasion.Node} - {invasion.DefendingFaction} vs. {invasion.AttackingFaction}",
                Description = $"*{invasion.Description}*",
                Color = CommandWFInfo.ActiveColor,
                Timestamp = invasion.StartTime
            };

            // Defender rewards
            string[] defenderRewards = invasion.DefenderReward?.ImportantRewardStrings().ToArray();
            if (defenderRewards != null && defenderRewards.Any())
                embed.AddField(invasion.DefendingFaction, string.Join("\n", defenderRewards));

            // Attacker rewards
            string[] attackerRewards = invasion.AttackerReward?.ImportantRewardStrings().ToArray();
            if (attackerRewards != null && attackerRewards.Any())
                embed.AddField(invasion.AttackingFaction, string.Join("\n", attackerRewards));

            return embed;
        }

        #region Verbs
        [Verb("cetus", HelpText = "Displays current time in Cetus")]
        public class CetusVerb : Verb {
            [Value(0, Required = false, MetaName = "body", HelpText = "Contents of the message (before the embed)")]
            public string Body { get; set; }

            public override async Task Execute(Command cmd, IMessage message, string[] args) {
                Task task = Task.CompletedTask;
                ConfigHandler.ConfigWrapper<Storage> config = cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(c => {
                    if (!c.CetusChannels.ContainsKey(message.Channel.Id)) {
                        c.CetusChannels.Add(message.Channel.Id, this.Body);
                        task = message.Reply("World state will now be tracked in this channel.");
                    } else if (c.CetusChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("World state will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });

                await task;
                await config.Save();
            }
        }

        [Verb("alerts", HelpText = "Displays Warframe alerts")]
        public class AlertsVerb : Verb {
            [Value(0, Required = false, MetaName = "body", HelpText = "Contents of the message (before the embed)")]
            public string Body { get; set; }

            public override async Task Execute(Command cmd, IMessage message, string[] args) {
                Task task = Task.CompletedTask;
                ConfigHandler.ConfigWrapper<Storage> config = cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(c => {
                    if (!c.AlertChannels.ContainsKey(message.Channel.Id)) {
                        c.AlertChannels.Add(message.Channel.Id, this.Body);
                        task = message.Reply("Alerts will now be tracked in this channel.");
                    } else if (c.AlertChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("Alerts will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });

                await task;
                await config.Save();
            }
        }

        [Verb("invasions", HelpText = "Displays Warframe invasions")]
        public class InvasionsVerb : Verb {
            [Value(0, Required = false, MetaName = "body", HelpText = "Contents of the message (before the embed)")]
            public string Body { get; set; }

            public override async Task Execute(Command cmd, IMessage message, string[] args) {
                Task task = Task.CompletedTask;
                ConfigHandler.ConfigWrapper<Storage> config = cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(c => {
                    if (!c.InvasionChannels.ContainsKey(message.Channel.Id)) {
                        c.InvasionChannels.Add(message.Channel.Id, this.Body);
                        task = message.Reply("Invasions will now be tracked in this channel.");
                    } else if (c.InvasionChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("Invasions will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });

                await task;
                await config.Save();
            }
        }
        #endregion

        public class Storage : IConfig {
            /// <summary>Key: channel ID, Value: message on alert</summary>
            public Dictionary<ulong, string> CetusChannels { get; set; } = new Dictionary<ulong, string>();
            /// <summary>Key: channel ID, Value: message on alert</summary>
            public Dictionary<ulong, string> AlertChannels { get; set; } = new Dictionary<ulong, string>();
            /// <summary>Key: channel ID, Value: message on alert</summary>
            public Dictionary<ulong, string> InvasionChannels { get; set; } = new Dictionary<ulong, string>();

            /// <summary>A set of messages that are tracking a cetus day/night cycle</summary>
            public HashSet<TimedMessageInfo> CetusMessages { get; set; } = new HashSet<TimedMessageInfo>();
            /// <summary>A set of messages that are tracking alerts</summary>
            public HashSet<TimedMessageInfo> AlertMessages { get; set; } = new HashSet<TimedMessageInfo>();
            /// <summary>A set of messages that are tracking invasions</summary>
            public HashSet<InvasionMessageInfo> InvasionMessages { get; set; } = new HashSet<InvasionMessageInfo>();
        }

        public class InvasionMessageInfo : TimedMessageInfo {
            public string InvasionID { get; set; }
        }

        #region Cetus Time
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

