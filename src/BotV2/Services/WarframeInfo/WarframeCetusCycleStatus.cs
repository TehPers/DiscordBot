using System;
using BotV2.Models.WarframeInfo;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Warframe.World.Models;

namespace BotV2.Services.WarframeInfo
{
    public class WarframeCetusCycleStatus : WarframeCycleStatus
    {
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;
        private readonly CetusCycle _cycle;

        protected override string CycleId => WarframeCetusCycle.CycleId;
        protected override string CycleName => WarframeCetusCycle.CycleName;
        protected override string Icon => (this._cycle.IsDay ? this._config.CurrentValue.CetusCycle?.DayIcon : this._config.CurrentValue.CetusCycle?.NightIcon) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} title icon");
        protected override string? Thumbnail => (this._cycle.IsDay ? this._config.CurrentValue.CetusCycle?.DayThumbnail : this._config.CurrentValue.CetusCycle?.NightThumbnail) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} thumbnail");
        protected override DiscordColor Color => new DiscordColor((this._cycle.IsDay ? this._config.CurrentValue.CetusCycle?.DayColor : this._config.CurrentValue.CetusCycle?.NightColor) ?? throw new Exception($"Config is missing a value for at least one {this.CycleName} message color"));

        public override string Id => this._cycle.IsDay ? "day" : "night";
        public override string Name => this._cycle.IsDay ? "Day" : "Night";
        public override DateTimeOffset Expiry => this._cycle.ExpiresAt;

        public WarframeCetusCycleStatus(WarframeInfoService infoService, IOptionsMonitor<WarframeInfoConfig> config, CetusCycle cycle) : base(infoService)
        {
            this._config = config;
            this._cycle = cycle ?? throw new ArgumentNullException(nameof(cycle));
        }
    }
}