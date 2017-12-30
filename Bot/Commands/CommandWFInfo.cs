using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Bot.Helpers;
using CommandLine;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using WarframeNET;
using static Bot.Helpers.MessageExtensions;

namespace Bot.Commands {
    public class CommandWFInfo : Command, IDisposable {
        public static string TrackedPlatform { get; } = Platform.PC;
        private const string ConfigName = "tracked";

        private static readonly TimeSpan HistoryLength = new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0);
        private static readonly Color ActiveColor = Color.DarkGreen;
        private static readonly Color ExpiredColor = Color.Red;
        private static readonly Color DayColor = new Color(255, 255, 0); // Yellow
        private static readonly Color NightColor = new Color(0, 0, 0); // Black

#if DEBUG
        private const int CetusUpdateRate = 10;
        private const int AlertsUpdateRate = 10;
        private const int InvasionsUpdateRate = 10;
#else
        private const int CetusUpdateRate = 60;
        private const int AlertsUpdateRate = 60;
        private const int InvasionsUpdateRate = 60;
#endif
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
            this.AddVerb<ToggleVerb>();
        }

        public override async Task Load() {
            // Prevent it from posting alerts as soon as the bot is reset
            WorldState state = await this._client.GetWorldStateAsync(CommandWFInfo.TrackedPlatform).ConfigureAwait(false);
            foreach (Alert alert in state.WS_Alerts)
                this._trackedIDs.Add(alert.Id);
            foreach (Invasion invasion in state.WS_Invasions)
                this._trackedIDs.Add(invasion.Id);
            this._day = CommandWFInfo.IsDay(DateTime.UtcNow);

            // Start tracking
            Bot.Instance.SecondsTimer.Elapsed += this.UpdateMessages;
        }

        public override Task Unload() {
            // Stop tracking
            Bot.Instance.SecondsTimer.Elapsed -= this.UpdateMessages;

            return base.Unload();
        }

        private async void UpdateMessages(object sender, ElapsedEventArgs elapsedEventArgs) {
            this._secondsElapsed++;

            List<Task> tasks = new List<Task>();
            if (this._secondsElapsed % CommandWFInfo.CetusUpdateRate == 0)
                tasks.Add(this.UpdateCetus());
            if (this._secondsElapsed % CommandWFInfo.AlertsUpdateRate == 0)
                tasks.Add(this.UpdateAlerts());
            if (this._secondsElapsed % CommandWFInfo.InvasionsUpdateRate == 0)
                tasks.Add(this.UpdateInvasions());

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task UpdateCetus() {
            ConfigHandler.ConfigWrapper<Storage> config = this.GetConfig<Storage>(CommandWFInfo.ConfigName);

            // Cetus Day/Night
            DateTime now = DateTime.UtcNow;
            bool day = CommandWFInfo.IsDay(now);
            if (day == this._day) {
                // Update messages
                TimedMessageInfo[] messages = config.GetValue(c => c.CetusMessages.ToArray());
                Embed embed = CommandWFInfo.GetCetusEmbed().Build();
                foreach (TimedMessageInfo messageInfo in messages) {
                    if (await messageInfo.GetMessage().ConfigureAwait(false) is IUserMessage msg) {
                        await msg.ModifySafe(m => m.Embed = embed).ConfigureAwait(false);
                    } else {
                        config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                    }
                }
            } else {
                this._day = day;

                // Delete current Cetus messages
                TimedMessageInfo[] messages = config.GetValue(c => c.CetusMessages.ToArray());
                foreach (TimedMessageInfo messageInfo in messages) {
                    if (await messageInfo.GetMessage().ConfigureAwait(false) is IUserMessage msg)
                        await msg.DeleteAsync().ConfigureAwait(false);

                    // Stop tracking the deleted message
                    config.SetValue(c => c.CetusMessages.Remove(messageInfo));
                }

                // Get all tracked channels
                IMessageChannel[] channels = config.GetValue(c => c.CetusChannels.ToArray())
                    .Select(id => Bot.Instance.GetChannel(id))
                    .Where(channel => channel is IMessageChannel)
                    .Cast<IMessageChannel>()
                    .ToArray();

                // Create new messages
                Embed embed = CommandWFInfo.GetCetusEmbed().Build();
                await Task.WhenAll(channels.Select(channel => {
                    IEnumerable<string> mentions = WFInfoVerb.GetRoles(this, channel.GetGuild(), day ? "day" : "night").Select(role => role.Mention);

                    // Send a new message
                    return channel.SendMessageAsync(string.Join(" ", mentions), embed: embed)

                        // Track it
                        .ContinueWith(task => {
                            config.SetValue(c => c.CetusMessages.Add(new TimedMessageInfo {
                                MessageID = task.Result.Id,
                                ChannelID = task.Result.Channel.Id,
                                DeleteTime = now + CommandWFInfo.CycleTimeLeft(now)
                            }));
                        });
                })).ConfigureAwait(false);
            }

            await config.Save().ConfigureAwait(false);
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
                    if (await messageInfo.GetMessage().ConfigureAwait(false) is IUserMessage msg) {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    } else {
                        config.SetValue(c => c.AlertMessages.Remove(messageInfo));
                    }
                } else if (messageInfo.ShouldExpire) {
                    // Make sure to modify the config properly
                    config.SetValue(c => messageInfo.Expired = true);

                    // Modify the message
                    if (await messageInfo.GetMessage().ConfigureAwait(false) is IUserMessage msg) {
                        await msg.ModifySafe(m => m.Embed = msg.Embeds.FirstOrDefault()?.AsBuilder().WithColor(CommandWFInfo.ExpiredColor).Build()).ConfigureAwait(false);
                    } else {
                        config.SetValue(c => c.AlertMessages.Remove(messageInfo));
                    }
                }
            }

            // Get tracked channels
            IMessageChannel[] channels = config.GetValue(c => c.AlertChannels.ToArray())
                .Select(id => Bot.Instance.GetChannel(id))
                .Where(channel => channel is IMessageChannel)
                .Cast<IMessageChannel>()
                .ToArray();

            // Post new alerts
            WorldState state = await this.GetWorldState().ConfigureAwait(false);
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
                    IEnumerable<string> mentions = WFInfoVerb.GetRoles(this, channel.GetGuild(), alert.Mission.Reward.ImportantRewards()).Select(r => r.Mention);

                    // Send a new message
                    return channel.SendMessageAsync(string.Join(" ", mentions), embed: embed)
                        // Track it
                        .ContinueWith(task => {
                            config.SetValue(c => c.AlertMessages.Add(new TimedMessageInfo {
                                MessageID = task.Result.Id,
                                ChannelID = task.Result.Channel.Id,
                                ExpireTime = alert.EndTime,
                                DeleteTime = alert.EndTime + CommandWFInfo.HistoryLength
                            }));
                        });
                })).ConfigureAwait(false);
            }

            await config.Save().ConfigureAwait(false);
        }

        private async Task UpdateInvasions() {
            ConfigHandler.ConfigWrapper<Storage> config = this.GetConfig<Storage>(CommandWFInfo.ConfigName);
            WorldState state = await this.GetWorldState().ConfigureAwait(false);

            // Update messages
            InvasionMessageInfo[] messages = config.GetValue(c => c.InvasionMessages.ToArray());
            foreach (InvasionMessageInfo messageInfo in messages) {
                if (messageInfo.ShouldDelete) {
                    // Make sure to modify the config properly
                    config.SetValue(c => c.InvasionMessages.Remove(messageInfo));

                    // Delete the message
                    if (await messageInfo.GetMessage().ConfigureAwait(false) is IUserMessage msg) {
                        await msg.DeleteAsync().ConfigureAwait(false);
                    }
                } else if (!messageInfo.Expired) {
                    // Get the invasion
                    Invasion invasion = state.WS_Invasions.FirstOrDefault(i => i.Id == messageInfo.InvasionID);

                    // Check if it's expired
                    if (invasion == null || Math.Abs(invasion.Completion) > 100) {
                        // Make sure to modify the config properly
                        config.SetValue(c => {
                            messageInfo.Expired = true;
                            messageInfo.DeleteTime = DateTimeOffset.UtcNow + CommandWFInfo.HistoryLength;
                        });

                        // Modify the message
                        if (await messageInfo.GetMessage().ConfigureAwait(false) is IUserMessage msg) {
                            await msg.ModifyAsync(m => m.Embed = msg.Embeds.FirstOrDefault()?.AsBuilder().WithColor(CommandWFInfo.ExpiredColor).Build()).ConfigureAwait(false);
                        } else {
                            config.SetValue(c => c.InvasionMessages.Remove(messageInfo));
                        }
                    }
                }
            }

            // Get tracked channels
            IMessageChannel[] channels = config.GetValue(c => c.InvasionChannels.ToArray())
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
                    IEnumerable<string> mentions = WFInfoVerb.GetRoles(this, channel.GetGuild(), invasion.AttackerReward.ImportantRewards().Concat(invasion.DefenderReward.ImportantRewards())).Select(r => r.Mention);

                    // Send a new message
                    return channel.SendMessageAsync(string.Join(" ", mentions), embed: embed)
                        // Track it
                        .ContinueWith(task => {
                            config.SetValue(c => c.InvasionMessages.Add(new InvasionMessageInfo {
                                MessageID = task.Result.Id,
                                ChannelID = task.Result.Channel.Id,
                                InvasionID = invasion.Id
                            }));
                        });
                })).ConfigureAwait(false);
            }

            await config.Save().ConfigureAwait(false);
        }

        private async Task<WorldState> GetWorldState() {
            // Make sure this code only happens once
            while (!await this._worldStateLock.WaitAsync(10000).ConfigureAwait(false))
                Bot.Instance.Log("Stuck in CommandWFInfo.GetWorldState(), will keep waiting");

            // Update the world state if necessary
            if (DateTimeOffset.Now - this._lastWorldState >= CommandWFInfo.WorldStateRate) {
                try {
                    this._lastWorldState = DateTimeOffset.Now;
                    this._worldState = await this._client.GetWorldStateAsync(CommandWFInfo.TrackedPlatform).ConfigureAwait(false);
                } catch (Exception ex) {
                    Bot.Instance.Log("Failed to download Warframe world state", LogSeverity.Warning, ex);
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
                Color = day ? CommandWFInfo.DayColor : CommandWFInfo.NightColor,
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
        public class CetusVerb : WFInfoVerb {
            public override async Task Execute(Command cmd, IUserMessage message, string[] args) {
                Task task = Task.CompletedTask;
                ConfigHandler.ConfigWrapper<Storage> config = cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(c => {
                    if (c.CetusChannels.Add(message.Channel.Id)) {
                        task = message.Reply("World state will now be tracked in this channel.");
                    } else if (c.CetusChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("World state will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });

                await task.ConfigureAwait(false);
                await config.Save().ConfigureAwait(false);
            }
        }

        [Verb("alerts", HelpText = "Displays Warframe alerts")]
        public class AlertsVerb : WFInfoVerb {
            public override async Task Execute(Command cmd, IUserMessage message, string[] args) {
                Task task = Task.CompletedTask;
                ConfigHandler.ConfigWrapper<Storage> config = cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(c => {
                    if (c.AlertChannels.Add(message.Channel.Id)) {
                        task = message.Reply("Alerts will now be tracked in this channel.");
                    } else if (c.AlertChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("Alerts will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });

                await task.ConfigureAwait(false);
                await config.Save().ConfigureAwait(false);
            }
        }

        [Verb("invasions", HelpText = "Displays Warframe invasions")]
        public class InvasionsVerb : WFInfoVerb {
            public override async Task Execute(Command cmd, IUserMessage message, string[] args) {
                Task task = Task.CompletedTask;
                ConfigHandler.ConfigWrapper<Storage> config = cmd.GetConfig<Storage>(CommandWFInfo.ConfigName).SetValue(c => {
                    if (c.InvasionChannels.Add(message.Channel.Id)) {
                        task = message.Reply("Invasions will now be tracked in this channel.");
                    } else if (c.InvasionChannels.Remove(message.Channel.Id)) {
                        task = message.Reply("Invasions will no longer be tracked in this channel.");
                    } else {
                        task = message.Reply("An error has occured.");
                    }
                });

                await task.ConfigureAwait(false);
                await config.Save().ConfigureAwait(false);
            }
        }

        [Verb("toggle", HelpText = "Toggles specific pings")]
        public class ToggleVerb : WFInfoVerb {
            [Value(0, Required = true, MetaName = "event", HelpText = "The type of alert (day, night, nitain extract, orokin reactor, etc.)")]
            public IEnumerable<string> Event { get; set; }

            public override async Task Execute(Command cmd, IUserMessage message, string[] args) {
                string item = string.Join(" ", this.Event).Trim();
                IGuild guild = message.GetGuild();

                IRole[] roles;
                try {
                    roles = (await WFInfoVerb.GetOrCreateRoles(cmd, guild, item).ConfigureAwait(false)).ToArray();
                } catch {
                    await message.Reply($"Unable to create role(s) for '{item}'", ReplyStatus.FAILURE).ConfigureAwait(false);
                    return;
                }

                if (!roles.Any()) {
                    await message.Reply($"Unknown item {item}", ReplyStatus.FAILURE).ConfigureAwait(false);
                    return;
                }

                IGuildUser user = await guild.GetUserAsync(message.Author.Id).ConfigureAwait(false);
                try {
                    IRole[] userRoles = user.RoleIds.Select(id => guild.GetRole(id)).ToArray();
                    IRole[] addedRoles = roles.Except(userRoles).ToArray();
                    IRole[] removedRoles = roles.Intersect(userRoles).ToArray();

                    if (addedRoles.Any())
                        await user.AddRolesAsync(addedRoles).ConfigureAwait(false);
                    if (removedRoles.Any())
                        await user.RemoveRolesAsync(removedRoles).ConfigureAwait(false);
                } catch {
                    await message.Reply($"Unable to manage role(s) for '{item}'", ReplyStatus.FAILURE).ConfigureAwait(false);
                    return;
                }

                await message.Reply(null, ReplyStatus.SUCCESS).ConfigureAwait(false);
            }
        }

        public abstract class WFInfoVerb : Verb {
            public static string[] Categories { get; } = {
                "nitain",
                "genetic",
                "catalyst",
                "reactor",
                "forma",
                "exilus",
                "sheev",
                "wraith",
                "vandal",
                "riven",
                "day",
                "night"
            };

            private static string GetRoleName(Command cmd, string category) {
                return $"{cmd.Name}: {category}";
            }

            #region GetRoles
            private static IRole InternalGetRoles(Command cmd, IGuild guild, string category) {
                string name = WFInfoVerb.GetRoleName(cmd, category);
                return guild.Roles.FirstOrDefault(r => r.Name == name);
            }

            public static IEnumerable<IRole> GetRoles(Command cmd, IGuild guild, IEnumerable<WarframeExtensions.StackedItem> items) => WFInfoVerb.GetRoles(cmd, guild, items.Select(i => i.Type));
            public static IEnumerable<IRole> GetRoles(Command cmd, IGuild guild, IEnumerable<string> items) {
                return items.SelectMany(i => WFInfoVerb.GetRoles(cmd, guild, i));
            }

            public static IEnumerable<IRole> GetRoles(Command cmd, IGuild guild, WarframeExtensions.StackedItem item) => WFInfoVerb.GetRoles(cmd, guild, item.Type);
            public static IEnumerable<IRole> GetRoles(Command cmd, IGuild guild, string item) {
                return WFInfoVerb.GetCategories(cmd, guild, item)
                    .Select(roleName => WFInfoVerb.InternalGetRoles(cmd, guild, roleName))
                    .Where(role => role != null);
            }
            #endregion

            #region GetCategories
            public static IEnumerable<string> GetCategories(Command cmd, IGuild guild, IEnumerable<WarframeExtensions.StackedItem> items) => WFInfoVerb.GetCategories(cmd, guild, items.Select(i => i.Type));
            public static IEnumerable<string> GetCategories(Command cmd, IGuild guild, IEnumerable<string> items) {
                return items.SelectMany(item => WFInfoVerb.GetCategories(cmd, guild, item));
            }

            public static IEnumerable<string> GetCategories(Command cmd, IGuild guild, WarframeExtensions.StackedItem item) => WFInfoVerb.GetCategories(cmd, guild, item.Type);
            public static IEnumerable<string> GetCategories(Command cmd, IGuild guild, string item) {
                return WFInfoVerb.Categories.Intersect(item.Split(), StringComparer.OrdinalIgnoreCase).Select(category => category.ToLower());
            }
            #endregion

            #region GetOrCreateRole
            private static async Task<IRole> GetOrCreateRole(Command cmd, IGuild guild, string category) {
                IRole role = WFInfoVerb.InternalGetRoles(cmd, guild, category);
                if (role == null) {
                    role = await guild.CreateRoleAsync(WFInfoVerb.GetRoleName(cmd, category)).ConfigureAwait(false);
                    await role.ModifyAsync(p => p.Mentionable = true).ConfigureAwait(false);
                }

                return role;
            }

            public static Task<IEnumerable<IRole>> GetOrCreateRoles(Command cmd, IGuild guild, IEnumerable<WarframeExtensions.StackedItem> items) => WFInfoVerb.GetOrCreateRoles(cmd, guild, items.Select(i => i.Type));
            public static Task<IEnumerable<IRole>> GetOrCreateRoles(Command cmd, IGuild guild, IEnumerable<string> items) {
                return Task.WhenAll(items.Select(item => WFInfoVerb.GetOrCreateRoles(cmd, guild, item)))
                    .ContinueWith(task => task.Result.SelectMany(roles => roles));
            }

            public static Task<IEnumerable<IRole>> GetOrCreateRoles(Command cmd, IGuild guild, WarframeExtensions.StackedItem item) => WFInfoVerb.GetOrCreateRoles(cmd, guild, item.Type);
            public static Task<IEnumerable<IRole>> GetOrCreateRoles(Command cmd, IGuild guild, string item) {
                return WFInfoVerb.InternalGetOrCreateRoles(cmd, guild, WFInfoVerb.GetCategories(cmd, guild, item));
            }

            private static Task<IEnumerable<IRole>> InternalGetOrCreateRoles(Command cmd, IGuild guild, IEnumerable<string> categories) {
                return Task.WhenAll(categories.Select(category => WFInfoVerb.GetOrCreateRole(cmd, guild, category))).ContinueWith(task => task.Result as IEnumerable<IRole>);
            }
            #endregion
        }
        #endregion

        public class Storage : IConfig {
            public HashSet<ulong> CetusChannels { get; set; } = new HashSet<ulong>();
            public HashSet<ulong> AlertChannels { get; set; } = new HashSet<ulong>();
            public HashSet<ulong> InvasionChannels { get; set; } = new HashSet<ulong>();

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

        public void Dispose() {
            this._worldStateLock?.Dispose();
        }
    }
}

