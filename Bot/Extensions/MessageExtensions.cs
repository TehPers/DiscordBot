using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.Win32.SafeHandles;

namespace Bot.Extensions
{
    public static class MessageExtensions
    {
        public static Task<IUserMessage> Reply(this IMessage msg, string reply) {
            return msg.Channel.SendMessageSafe($"{msg.Author.Mention} {reply}");
        }

        public static Task SendToAll(this IEnumerable<IMessageChannel> channels, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null) {
            return Task.WhenAll(channels.Select(channel => channel.SendMessageSafe(text, isTTS, embed, options)));
        }

        public static async Task<IUserMessage> SendMessageSafe(this IMessageChannel channel, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null) {
            // Make sure the bot is in this channel
            IUser user = await channel.GetUserAsync(Bot.Instance.Client.CurrentUser.Id, options: options);

            // Send the message
            return user == null ? null : await channel.SendMessageAsync(text, isTTS, embed, options);
        }

        public static IGuild GetGuild(this IMessage msg) => msg.Channel.GetGuild();
        public static IGuild GetGuild(this IChannel channel) {
            return channel is IGuildChannel guildChannel ? guildChannel.Guild : null;
        }
    }
}
