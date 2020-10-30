﻿using System;
using System.Linq;
using BotV2.Extensions;
using DSharpPlus.Entities;

namespace BotV2.Services.WarframeInfo
{
    public abstract class WarframeCycleStatus : IWarframeCycleStatus
    {
        private readonly WarframeInfoService _infoService;

        protected abstract string CycleId { get; }
        protected abstract string CycleName { get; }
        protected abstract string TitleIcon { get; }
        protected abstract string? Thumbnail { get; }
        protected abstract DiscordColor Color { get; }

        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract DateTimeOffset Expiry { get; }

        protected WarframeCycleStatus(WarframeInfoService infoService)
        {
            this._infoService = infoService;
        }

        public (string message, DiscordEmbed embed) GetMessage(DiscordChannel channel)
        {
            var roles = this._infoService.GetRolesForCycle(channel.Guild, this.CycleId, this.Id).Distinct();
            var content = string.Join(" ", roles.Where(role => role.IsMentionable).Select(role => role.Mention));
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{this.TitleIcon} {this.CycleName}")
                .WithDescription($"{this.Name} time remaining: {(this.Expiry - DateTimeOffset.UtcNow).FormatWarframeTime()}")
                .WithColor(this.Color)
                .WithFooter("End time")
                .WithTimestamp(this.Expiry);

            if (!string.IsNullOrWhiteSpace(this.Thumbnail))
            {
                embed = embed.WithThumbnail(this.Thumbnail);
            }

            return (content, embed);
        }
    }
}