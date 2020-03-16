using System;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Services;
using BotV2.Services.Messages;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BotV2.CommandModules.Warframe
{
    [Group("wfinfo")]
    [RequireOwner]
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
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeInfoService.InfoType.Alerts);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Alerts {(enabled ? "enabled" : "disabled")}");
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
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeInfoService.InfoType.Invasions);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Invasions {(enabled ? "enabled" : "disabled")}");
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
            public async Task Toggle(CommandContext context)
            {
                _ = context ?? throw new ArgumentNullException(nameof(context));

                var enabled = await this._infoService.ToggleSubscription(context.Channel.Id, WarframeInfoService.InfoType.Cetus);
                await this._timedMessageService.TimedRespondAsync(context, DateTimeOffset.UtcNow + WarframeInfoModule.ResponseExpiry, $"Cetus day/night cycles {(enabled ? "enabled" : "disabled")}");
            }
        }
    }
}