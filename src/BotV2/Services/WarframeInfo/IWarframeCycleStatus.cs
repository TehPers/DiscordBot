using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace BotV2.Services.WarframeInfo
{
    public interface IWarframeCycleStatus
    {
        string Id { get; }

        string Name { get; }

        DateTimeOffset Expiry { get; }

        Task<(string message, DiscordEmbed embed)> GetMessage(DiscordClient client, DiscordChannel channel);
    }
}