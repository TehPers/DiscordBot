using System;
using System.Threading.Tasks;
using BotV2.Models;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace BotV2.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<Option<DiscordMessage>> TryModifyAsync(this DiscordMessage message, Optional<string> content = default, Optional<DiscordEmbed> embed = default)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            try
            {
                return new Option<DiscordMessage>(await message.ModifyAsync(content, embed));
            }
            catch (NotFoundException)
            {
                return default;
            }
        }

        public static async Task<bool> TryDeleteAsync(this DiscordMessage message, string? reason = default)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));

            try
            {
                await message.DeleteAsync(reason);
                return true;
            }
            catch (NotFoundException)
            {
                return false;
            }
        }
    }
}