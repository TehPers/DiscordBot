using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BotV2.BotExtensions;
using BotV2.Extensions;
using BotV2.Services.Messages;
using BotV2.Services.WarframeInfo;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BotV2.CommandModules.Warframe
{
    [Group("wfinfo")]
    [RequireOwner]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Methods are called via reflection.")]
    public sealed class WarframeInfoModule : BaseCommandModule
    {
        private static readonly TimeSpan ResponseExpiry = TimeSpan.FromMinutes(0.25);

        [Group("alerts")]
        public sealed class AlertsGroup : BaseCommandModule
        {
            private readonly WarframeInfoService _infoService;
            private readonly TimedMessageService _timedMessageService;

            public AlertsGroup(WarframeInfoService infoService, TimedMessageService timedMessageService)
            {
                this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
                this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            }

            [Command("toggle")]
            [RequireBotPermissions(Permissions.SendMessages)]
            [RequireGuild]
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeInfoBotExtension.AlertsSubscriberKey).ConfigureAwait(false);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Alerts {(enabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
        }

        [Group("invasions")]
        public sealed class InvasionsGroup : BaseCommandModule
        {
            private readonly WarframeInfoService _infoService;
            private readonly TimedMessageService _timedMessageService;

            public InvasionsGroup(WarframeInfoService infoService, TimedMessageService timedMessageService)
            {
                this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
                this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            }

            [Command("toggle")]
            [RequireBotPermissions(Permissions.SendMessages)]
            [RequireGuild]
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeInfoBotExtension.InvasionsSubscriberKey).ConfigureAwait(false);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Invasions {(enabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
        }

        [Group("earth")]
        public sealed class EarthGroup : BaseCommandModule
        {
            private readonly WarframeInfoService _infoService;
            private readonly TimedMessageService _timedMessageService;

            public EarthGroup(WarframeInfoService infoService, TimedMessageService timedMessageService)
            {
                this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
                this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            }

            [Command("toggle")]
            [RequireBotPermissions(Permissions.SendMessages)]
            [RequireGuild]
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeEarthCycle.CycleId).ConfigureAwait(false);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Earth day/night cycles {(enabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
        }

        [Group("cetus")]
        public sealed class CetusGroup : BaseCommandModule
        {
            private readonly WarframeInfoService _infoService;
            private readonly TimedMessageService _timedMessageService;

            public CetusGroup(WarframeInfoService infoService, TimedMessageService timedMessageService)
            {
                this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
                this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            }

            [Command("toggle")]
            [RequireBotPermissions(Permissions.SendMessages)]
            [RequireGuild]
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeCetusCycle.CycleId).ConfigureAwait(false);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Cetus day/night cycles {(enabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
        }

        [Group("vallis")]
        public sealed class VallisGroup : BaseCommandModule
        {
            private readonly WarframeInfoService _infoService;
            private readonly TimedMessageService _timedMessageService;

            public VallisGroup(WarframeInfoService infoService, TimedMessageService timedMessageService)
            {
                this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
                this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            }

            [Command("toggle")]
            [RequireBotPermissions(Permissions.SendMessages)]
            [RequireGuild]
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeVallisCycle.CycleId).ConfigureAwait(false);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Orb Vallis warm/cold cycles {(enabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
        }

        [Group("cambion")]
        public sealed class CambionGroup : BaseCommandModule
        {
            private readonly WarframeInfoService _infoService;
            private readonly TimedMessageService _timedMessageService;

            public CambionGroup(WarframeInfoService infoService, TimedMessageService timedMessageService)
            {
                this._infoService = infoService ?? throw new ArgumentNullException(nameof(infoService));
                this._timedMessageService = timedMessageService ?? throw new ArgumentNullException(nameof(timedMessageService));
            }

            [Command("toggle")]
            [RequireBotPermissions(Permissions.SendMessages)]
            [RequireGuild]
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeCambionCycle.CycleId).ConfigureAwait(false);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Cambion Drift fass/vome cycles {(enabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            }
        }
    }
}