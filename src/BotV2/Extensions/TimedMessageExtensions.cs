using System;
using System.Threading.Tasks;
using BotV2.BotExtensions;
using BotV2.Services.Messages;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotV2.Extensions
{
    public static class TimedMessageExtensions
    {
        public static IServiceCollection AddTimedMessages(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<TimedMessageService>();
            services.TryAddBotExtension<TimedMessageBotExtension>();
            return services;
        }

        public static Task<DiscordMessage> TimedRespondAsync(this TimedMessageService timedMessages, CommandContext context, DateTimeOffset removeAfter, string? content = default, bool tts = false, DiscordEmbed? embed = default)
        {
            return timedMessages.TimedRespondAsync(context.Message, removeAfter, content, tts, embed);
        }

        public static async Task<DiscordMessage> TimedRespondAsync(this TimedMessageService timedMessages, DiscordMessage message, DateTimeOffset removeAfter, string? content = default, bool tts = false, DiscordEmbed? embed = default)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            _ = timedMessages ?? throw new ArgumentNullException(nameof(timedMessages));

            var reply = await message.RespondAsync(content, tts, embed);
            await timedMessages.RemoveAfter(reply, removeAfter);
            return reply;
        }

        public static async Task<DiscordMessage> TimedSendMessageAsync(this TimedMessageService timedMessages, DiscordChannel channel, DateTimeOffset removeAfter, string? content = default, bool tts = false, DiscordEmbed? embed = default)
        {
            _ = channel ?? throw new ArgumentNullException(nameof(channel));
            _ = timedMessages ?? throw new ArgumentNullException(nameof(timedMessages));

            var message = await channel.SendMessageAsync(content, tts, embed);
            await timedMessages.RemoveAfter(message, removeAfter);
            return message;
        }
    }
}