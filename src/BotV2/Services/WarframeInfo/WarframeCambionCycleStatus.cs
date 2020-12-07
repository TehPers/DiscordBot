using System;
using BotV2.Models.WarframeInfo;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Warframe.World.Models;

namespace BotV2.Services.WarframeInfo
{
    public class WarframeCambionCycleStatus : WarframeCycleStatus
    {
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;
        private readonly CambionCycle _cycle;

        protected override string CycleId => WarframeCambionCycle.CycleId;
        protected override string CycleName => WarframeCambionCycle.CycleName;
        protected override string Icon => (this._cycle.IsFass ? this._config.CurrentValue.CambionCycle?.FassIcon : this._config.CurrentValue.CambionCycle?.VomeIcon) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} title icon");
        protected override string? Thumbnail => (this._cycle.IsFass ? this._config.CurrentValue.CambionCycle?.FassThumbnail : this._config.CurrentValue.CambionCycle?.VomeThumbnail) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} thumbnail");
        protected override DiscordColor Color => new DiscordColor((this._cycle.IsFass ? this._config.CurrentValue.CambionCycle?.FassColor : this._config.CurrentValue.CambionCycle?.VomeColor) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} message color"));

        public override string Id => this._cycle.IsFass ? "fass" : "vome";
        public override string Name => this._cycle.IsFass ? "Fass" : "Vome";
        public override DateTimeOffset Expiry => this._cycle.ExpiresAt;

        public WarframeCambionCycleStatus(WarframeInfoService infoService, IOptionsMonitor<WarframeInfoConfig> config, CambionCycle cycle) : base(infoService)
        {
            this._config = config;
            this._cycle = cycle ?? throw new ArgumentNullException(nameof(cycle));
        }
    }
}