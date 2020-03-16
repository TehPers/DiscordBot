using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Models.WarframeInfo;
using BotV2.Services;
using BotV2.Services.Data;
using BotV2.Services.Messages;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warframe;
using Warframe.World.Models;

namespace BotV2.BotExtensions
{
    public sealed class WarframeInfoBotExtension : MultiThreadedBotExtension
    {
        private const string BaseKey = "wfinfo";

        private const string AlertsKey = WarframeInfoBotExtension.BaseKey + ":alerts";
        private const string InvasionsKey = WarframeInfoBotExtension.BaseKey + ":invasions";
        private const string CetusKey = WarframeInfoBotExtension.BaseKey + ":cetus";

        private const string ProcessedAlertsKey = WarframeInfoBotExtension.AlertsKey + ":processed";
        private const string ProcessedInvasionsKey = WarframeInfoBotExtension.InvasionsKey + ":processed";
        private const string AlertExpiryKey = WarframeInfoBotExtension.AlertsKey + ":expire";
        private const string ActiveInvasionsKey = WarframeInfoBotExtension.InvasionsKey + ":active";
        private const string CetusMessagesKey = WarframeInfoBotExtension.CetusKey + ":messages";
        private const string CetusStatusKey = WarframeInfoBotExtension.CetusKey + ":last_status";

        private readonly TimeSpan _historyLength = TimeSpan.FromDays(3);
        private readonly TimeSpan _pollRate = TimeSpan.FromMinutes(0.5);
        private readonly TimeSpan _invasionTtl = TimeSpan.FromDays(7);

        private readonly WarframeInfoService _infoService;
        private readonly WarframeClient _wfClient;
        private readonly IDataService _dataService;
        private readonly ILogger<WarframeInfoBotExtension> _logger;
        private readonly TimedMessageService _timedMessageService;
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;

        public WarframeInfoBotExtension(WarframeInfoService infoService, WarframeClient wfClient, IDataService dataService, ILogger<WarframeInfoBotExtension> logger, TimedMessageService timedMessageService, IOptionsMonitor<WarframeInfoConfig> config)
        {
            this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
            this._wfClient = wfClient ?? throw new ArgumentNullException(nameof(wfClient));
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        protected override void Setup(DiscordClient client)
        {
            base.Setup(client);

            client.Ready += args =>
            {
                this.RunParallel(this.Monitor);
                return Task.CompletedTask;
            };
        }

        [SuppressMessage("ReSharper", "FunctionNeverReturns", Justification = "Function exits when the cancellation token is cancelled")]
        private async Task Monitor(CancellationToken cancellation)
        {
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();

                try
                {
                    using var timedSource = new CancellationTokenSource(this._pollRate * 1.5);
                    using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation, timedSource.Token);

                    await Task.WhenAll(
                        this.GenerateAlertMessages(combinedSource.Token),
                        this.GenerateInvasionMessages(combinedSource.Token),
                        this.GenerateCetusMessages(combinedSource.Token),
                        this.ExpireAlerts(combinedSource.Token),
                        this.ExpireInvasions(combinedSource.Token),
                        this.UpdateCetusMessages(combinedSource.Token),
                        Task.Delay(this._pollRate, combinedSource.Token)
                    );
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "An exception occurred while monitoring the warframe world state");
                }
            }
        }

        private async IAsyncEnumerable<Alert> GetNewAlerts([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            var globalStore = this._dataService.GetGlobalStore();
            foreach (var alert in await this._wfClient.GetAlertsAsync(cancellation).ConfigureAwait(false))
            {
                if (alert.HasExpired)
                {
                    continue;
                }

                var importantRewards = this._infoService.GetImportantRewards(alert.Mission.Reward);
                if (!importantRewards.Any())
                {
                    continue;
                }

                var processedMarker = globalStore.GetObjectResource<bool>($"{WarframeInfoBotExtension.ProcessedAlertsKey}:{alert.Id}");
                if ((await processedMarker.Set(true)).TryGetValue(out var wasAlreadyProcessed) && wasAlreadyProcessed)
                {
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                var expiryBase = now > alert.ExpiresAt ? now : alert.ExpiresAt;
                await processedMarker.SetExpiry(expiryBase + this._historyLength * 2 + this._pollRate * 2);
                yield return alert;
            }
        }

        private async Task GenerateAlertMessages(CancellationToken cancellation = default)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var alertExpiry = globalStore.GetDelayedTaskQueueResource<MessagePointer>(WarframeInfoBotExtension.AlertExpiryKey);
            var newAlerts = await this.GetNewAlerts(cancellation).ToListAsync(cancellation);
            await foreach (var subscriber in this._infoService.GetSubscribers(WarframeInfoService.InfoType.Alerts).WithCancellation(cancellation))
            {
                foreach (var alert in newAlerts)
                {
                    var channel = await this.Client.GetChannelAsync(subscriber);
                    var embed = this.GetAlertEmbed(alert);
                    var msg = await channel.SendMessageAsync(embed: embed);
                    var endTime = alert.ExpiresAt;

                    try
                    {
                        await this._timedMessageService.RemoveAfter(msg, endTime + this._historyLength);
                        await alertExpiry.AddAsync(new MessagePointer(msg), endTime + this._historyLength);
                        this._logger.LogTrace($"Created message for alert {alert.Id} in {subscriber}.");
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(ex, $"Unable to create alert message for subscriber {subscriber} and alert {alert.Id}");
                    }
                }
            }
        }

        private async IAsyncEnumerable<Invasion> GetNewInvasions([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            var globalStore = this._dataService.GetGlobalStore();
            foreach (var invasion in await this._wfClient.GetInvasionsAsync(cancellation).ConfigureAwait(false))
            {
                if (invasion.Completed)
                {
                    continue;
                }

                var importantAttackerRewards = this._infoService.GetImportantRewards(invasion.AttackerReward);
                var importantDefenderRewards = this._infoService.GetImportantRewards(invasion.DefenderReward);
                if (!importantAttackerRewards.Any() && !importantDefenderRewards.Any())
                {
                    continue;
                }

                var processedMarker = globalStore.GetObjectResource<object?>($"{WarframeInfoBotExtension.ProcessedInvasionsKey}:{invasion.Id}");
                if (!await processedMarker.TrySet(new object()))
                {
                    continue;
                }

                var now = DateTimeOffset.UtcNow;
                var expiryBase = now > invasion.ActivatedAt ? now : invasion.ActivatedAt;
                await processedMarker.SetExpiry(expiryBase + this._invasionTtl * 2 + this._pollRate * 2);
                yield return invasion;
            }
        }

        private async Task GenerateInvasionMessages(CancellationToken cancellation = default)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var newInvasions = await this.GetNewInvasions(cancellation).ToListAsync(cancellation);
            await foreach (var subscriber in this._infoService.GetSubscribers(WarframeInfoService.InfoType.Invasions).WithCancellation(cancellation))
            {
                foreach (var invasion in newInvasions)
                {
                    var embed = this.GetInvasionEmbed(invasion);
                    var channel = await this.Client.GetChannelAsync(subscriber);
                    var msg = await channel.SendMessageAsync(embed: embed);
                    var removeAfter = invasion.ActivatedAt + this._invasionTtl;

                    try
                    {
                        // Track the invasion
                        var invasionMessages = globalStore.GetSetResource<MessagePointer>($"{WarframeInfoBotExtension.ActiveInvasionsKey}:{invasion.Id}");
                        await invasionMessages.AddAsync(new MessagePointer(msg));
                        await invasionMessages.SetExpiry(removeAfter);

                        // Automatically remove invasion alert after a while
                        await this._timedMessageService.RemoveAfter(msg, removeAfter);

                        this._logger.LogTrace($"Created message for invasion {invasion.Id} in {subscriber}.");
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(ex, $"Unable to create invasion message for subscriber {subscriber} and invasion {invasion.Id}");
                    }
                }
            }
        }

        private async Task GenerateCetusMessages(CancellationToken cancellation = default)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var lastStatus = globalStore.GetObjectResource<bool>(WarframeInfoBotExtension.CetusStatusKey);
            var cetusMessages = globalStore.GetSetResource<MessagePointer>(WarframeInfoBotExtension.CetusMessagesKey);
            var cetusStatus = await this._wfClient.GetCetusStatus(cancellation);

            if ((await lastStatus.Set(cetusStatus.IsDay)).TryGetValue(out var lastStatusValue) && lastStatusValue == cetusStatus.IsDay)
            {
                return;
            }

            await foreach (var messagePointer in cetusMessages.PopAll(cancellation))
            {
                try
                {
                    if (await messagePointer.TryGetMessage(this.Client) is {} message)
                    {
                        await message.TryDeleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"Unable to delete cetus message {messagePointer}");
                }
            }

            await foreach (var subscriber in this._infoService.GetSubscribers(WarframeInfoService.InfoType.Cetus).WithCancellation(cancellation))
            {
                var embed = this.GetCetusEmbed(cetusStatus);
                var channel = await this.Client.GetChannelAsync(subscriber);
                var msg = await channel.SendMessageAsync(embed: embed);

                try
                {
                    await this._timedMessageService.RemoveAfter(msg, cetusStatus.ExpiresAt + this._pollRate * 2 + TimeSpan.FromMinutes(5));
                    await cetusMessages.AddAsync(new MessagePointer(msg));
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"Unable to create cetus message for subscriber {subscriber}");
                }
            }
        }

        private async Task ExpireAlerts(CancellationToken cancellation)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var alertExpiry = globalStore.GetDelayedTaskQueueResource<MessagePointer>(WarframeInfoBotExtension.AlertExpiryKey);
            await foreach (var messagePointer in alertExpiry.PopAvailable(cancellation))
            {
                try
                {
                    // Try to mark the message as expired
                    if (await messagePointer.TryGetMessage(this.Client) is { } message && message.Embeds.FirstOrDefault() is { } embed)
                    {
                        var builder = new DiscordEmbedBuilder(embed).WithColor(new DiscordColor(this._config.CurrentValue.ExpiredColor ?? "#000000"));
                        await message.ModifyAsync(embed: builder.Build());
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"Unable to modify alert message {messagePointer}");
                }
            }
        }

        private async Task ExpireInvasions(CancellationToken cancellation)
        {
            var globalStore = this._dataService.GetGlobalStore();
            foreach (var invasion in await this._wfClient.GetInvasionsAsync(cancellation))
            {
                if (!invasion.Completed)
                {
                    continue;
                }

                // Get all messages being expired
                var invasionMessages = globalStore.GetSetResource<MessagePointer>($"{WarframeInfoBotExtension.ActiveInvasionsKey}:{invasion.Id}");
                await foreach (var messagePointer in invasionMessages.PopAll(cancellation))
                {
                    try
                    {
                        // Try to mark the message as expired
                        if (await messagePointer.TryGetMessage(this.Client) is { } message && message.Embeds.FirstOrDefault() is { } embed)
                        {
                            await this._timedMessageService.RemoveAfter(message, DateTimeOffset.UtcNow + this._historyLength);

                            var builder = new DiscordEmbedBuilder(embed).WithColor(new DiscordColor(this._config.CurrentValue.ExpiredColor ?? "#000000"));
                            await message.TryModifyAsync(embed: builder.Build());
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(ex, $"Unable to modify invasion message {messagePointer}");
                    }
                }
            }
        }

        private async Task UpdateCetusMessages(CancellationToken cancellation)
        {
            var globalStore = this._dataService.GetGlobalStore();
            var cetusMessages = globalStore.GetSetResource<MessagePointer>(WarframeInfoBotExtension.CetusMessagesKey);
            var cetusStatus = await this._wfClient.GetCetusStatus(cancellation);
            var embed = this.GetCetusEmbed(cetusStatus);

            await foreach (var messagePointer in cetusMessages)
            {
                try
                {
                    if (await messagePointer.TryGetMessage(this.Client) is { } message)
                    {
                        await message.TryModifyAsync(embed: embed);
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, $"Unable to modify cetus message {messagePointer}");
                }
            }
        }

        private DiscordEmbed GetAlertEmbed(Alert alert)
        {
            _ = alert ?? throw new ArgumentNullException(nameof(alert));

            var description = new StringBuilder();
            if (alert.Mission.IsNightmare)
            {
                description.AppendLine("**NIGHTMARE** (No Shields)");
            }

            if (alert.Mission.IsArchwingRequired)
            {
                description.AppendLine("Archwing Required");
            }

            description.AppendLine($"{alert.Mission.Type} - {alert.Mission.Faction} ({alert.Mission.MinimumEnemyLevel}-{alert.Mission.MaximumEnemyLevel})");
            description.AppendLine(string.Join(", ", alert.Mission.Reward.Forward(this._infoService.GetImportantRewards).Forward(this._infoService.GetItemStrings)));

            var durationStr = this.Format(alert.ExpiresAt - alert.ActivatedAt);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Alert - {alert.Mission.Node} - {durationStr}")
                .WithDescription(description.ToString())
                .WithColor(new DiscordColor(this._config.CurrentValue.ActiveColor ?? "#000000"))
                .WithTimestamp(alert.ActivatedAt)
                .WithFooter(text: durationStr);

            // Thumbnail
            if (this._config.CurrentValue.RewardIcons is { } rewardThumbnails)
            {
                foreach (var reward in this._infoService.GetImportantRewards(alert.Mission.Reward))
                {
                    if (rewardThumbnails.FirstOrDefault(kv => string.Equals(kv.Key, reward.Type, StringComparison.OrdinalIgnoreCase)) is {Value: string thumbnail} && !string.IsNullOrWhiteSpace(thumbnail))
                    {
                        embed = embed.WithThumbnailUrl(thumbnail);
                        break;
                    }
                }
            }

            return embed;
        }

        private DiscordEmbed GetInvasionEmbed(Invasion invasion)
        {
            _ = invasion ?? throw new ArgumentNullException(nameof(invasion));

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Invasion - {invasion.Node} - {invasion.DefendingFaction} vs. {invasion.AttackingFaction}")
                .WithDescription($"*{invasion.Description}*")
                .WithColor(new DiscordColor(this._config.CurrentValue.ActiveColor))
                .WithTimestamp(invasion.ActivatedAt);

            // Defender rewards
            var defenderRewards = invasion.DefenderReward.Forward(this._infoService.GetImportantRewards).Forward(this._infoService.GetItemStrings).ToArray();
            if (defenderRewards.Any())
            {
                embed.AddField(invasion.DefendingFaction, string.Join("\n", defenderRewards));
            }

            // Attacker rewards
            var attackerRewards = invasion.DefenderReward.Forward(this._infoService.GetImportantRewards).Forward(this._infoService.GetItemStrings).ToArray();
            if (attackerRewards.Any())
            {
                embed.AddField(invasion.AttackingFaction, string.Join("\n", attackerRewards));
            }

            // Thumbnail
            if (this._config.CurrentValue.FactionThumbnails is {} factionThumbnails && factionThumbnails.FirstOrDefault(kv => string.Equals(kv.Key, invasion.AttackingFaction, StringComparison.OrdinalIgnoreCase)) is {Value: string thumbnail} && !string.IsNullOrWhiteSpace(thumbnail))
            {
                embed = embed.WithThumbnailUrl(thumbnail);
            }

            return embed;
        }

        private DiscordEmbed GetCetusEmbed(CetusCycle cetusStatus)
        {
            var color = cetusStatus.IsDay ? this._config.CurrentValue.DayColor : this._config.CurrentValue.NightColor;
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{(cetusStatus.IsDay ? this._config.CurrentValue.DayIcon : this._config.CurrentValue.NightIcon)} Cetus")
                .WithDescription($"{(cetusStatus.IsDay ? "Day" : "Night")} time remaining: {this.Format(cetusStatus.ExpiresAt - DateTimeOffset.UtcNow)}")
                .WithColor(new DiscordColor(color ?? "#000000"))
                .WithTimestamp(cetusStatus.ActivatedAt);

            // Thumbnail
            var thumbnail = cetusStatus.IsDay ? this._config.CurrentValue.DayThumbnail : this._config.CurrentValue.NightThumbnail;
            if (!string.IsNullOrWhiteSpace(thumbnail))
            {
                embed = embed.WithThumbnailUrl(thumbnail);
            }

            return embed;
        }

        private string Format(TimeSpan interval)
        {
            var result = new StringBuilder();

            if (interval.Days > 0)
            {
                result.Append($"{interval.Days}d ");
            }

            if (interval.Hours > 0)
            {
                result.Append($"{interval.Hours}h ");
            }

            if (interval.Minutes > 0 || interval.TotalMinutes < 1)
            {
                result.Append($"{Math.Ceiling(interval.TotalMinutes % 60)}m");
            }

            return result.ToString();
        }
    }
}