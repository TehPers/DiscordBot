using System;
using System.Linq;
using System.Threading.Tasks;
using BotV2.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;

namespace BotV2.Services.WarframeInfo
{
    public abstract class WarframeCycleStatus : IWarframeCycleStatus
    {
        private readonly WarframeInfoService _infoService;

        protected abstract string CycleId { get; }
        protected abstract string CycleName { get; }
        protected abstract string Icon { get; }
        protected abstract string? Thumbnail { get; }
        protected abstract DiscordColor Color { get; }

        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract DateTimeOffset Expiry { get; }

        protected WarframeCycleStatus(WarframeInfoService infoService)
        {
            this._infoService = infoService;
        }

        public async Task<(string message, DiscordEmbed embed)> GetMessage(DiscordClient client, DiscordChannel channel)
        {
            var roles = this._infoService.GetRolesForCycle(channel.Guild, this.CycleId, this.Id).Distinct();
            if (!(channel.Guild is { } guild) || !(await guild.GetMemberAsync(client.CurrentUser.Id).ConfigureAwait(false) is { } member) || !member.PermissionsIn(channel).HasFlag(Permissions.MentionEveryone))
            {
                roles = roles.Where(role => role.IsMentionable);
            }

            var content = string.Join(" ", roles.Select(role => role.Mention));
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"{this.CycleName}")
                .WithDescription($"{this.Icon} `{this.Name.ToUpper()}`\nTime remaining: {(this.Expiry - DateTimeOffset.UtcNow).FormatWarframeTime()}")
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