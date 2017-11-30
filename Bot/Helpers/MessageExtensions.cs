using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Bot.Helpers {
    public static class MessageExtensions {
        public static Task<IUserMessage> Reply(this IMessage msg, string reply) {
            return msg.Channel.SendMessageSafe($"{msg.Author.Mention} {reply}");
        }

        public static Task<IUserMessage[]> SendToAll(this IEnumerable<IMessageChannel> channels, string text, bool isTTS = false, Embed embed = null, RequestOptions options = null) {
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

        public static Task<IMessageChannel> GetMessageChannel(this IDiscordClient client, ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) {
            return client.GetChannelAsync(id, mode, options).ContinueWith(task => task.Result as IMessageChannel);
        }

        public static EmbedBuilder AsBuilder(this IEmbed embed) {
            return new EmbedBuilder {
                Author = embed.Author?.AsBuilder(),
                Color = embed.Color,
                Description = embed.Description,
                Fields = embed.Fields.Select(f => f.AsBuilder()).ToList(),
                Footer = embed.Footer?.AsBuilder(),
                ImageUrl = embed.Image?.Url,
                ThumbnailUrl = embed.Thumbnail?.Url,
                Timestamp = embed.Timestamp,
                Title = embed.Title,
                Url = embed.Url
            };
        }

        public static EmbedFieldBuilder AsBuilder(this EmbedField field) {
            return new EmbedFieldBuilder {
                IsInline = field.Inline,
                Name = field.Name,
                Value = field.Value
            };
        }

        public static EmbedAuthorBuilder AsBuilder(this EmbedAuthor author) {
            return new EmbedAuthorBuilder {
                IconUrl = author.IconUrl,
                Name = author.Name,
                Url = author.Url
            };
        }

        public static EmbedFooterBuilder AsBuilder(this EmbedFooter footer) {
            return new EmbedFooterBuilder {
                IconUrl = footer.IconUrl,
                Text = footer.Text
            };
        }
    }
}
