using System;
using BotV2.Models.WarframeInfo;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Warframe.World.Models;

namespace BotV2.Services.WarframeInfo
{
    public class WarframeVallisCycleStatus : WarframeCycleStatus
    {
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;
        private readonly VallisCycle _cycle;

        protected override string CycleId => WarframeVallisCycle.CycleId;
        protected override string CycleName => WarframeVallisCycle.CycleName;
        protected override string TitleIcon => (this._cycle.IsWarm ? this._config.CurrentValue.VallisCycle?.WarmIcon : this._config.CurrentValue.VallisCycle?.ColdIcon) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} title icon");
        protected override string? Thumbnail => (this._cycle.IsWarm ? this._config.CurrentValue.VallisCycle?.WarmThumbnail : this._config.CurrentValue.VallisCycle?.ColdThumbnail) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} thumbnail");
        protected override DiscordColor Color => new DiscordColor((this._cycle.IsWarm ? this._config.CurrentValue.VallisCycle?.WarmColor : this._config.CurrentValue.VallisCycle?.ColdColor) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} message color"));

        public override string Id => this._cycle.IsWarm ? "warm" : "cold";
        public override string Name => this._cycle.IsWarm ? "Warm" : "Cold";
        public override DateTimeOffset Expiry => this._cycle.ExpiresAt;

        public WarframeVallisCycleStatus(WarframeInfoService infoService, IOptionsMonitor<WarframeInfoConfig> config, VallisCycle cycle) : base(infoService)
        {
            this._config = config;
            this._cycle = cycle ?? throw new ArgumentNullException(nameof(cycle));
        }
    }
}