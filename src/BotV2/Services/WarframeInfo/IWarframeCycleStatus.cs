using System;
using DSharpPlus.Entities;

namespace BotV2.Services.WarframeInfo
{
    public interface IWarframeCycleStatus
    {
        string Id { get; }

        string Name { get; }

        DateTimeOffset Expiry { get; }

        (string message, DiscordEmbed embed) GetMessage(DiscordChannel channel);
    }
}