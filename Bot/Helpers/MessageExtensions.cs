﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Commands;
using Discord;
using Discord.Net;

namespace Bot.Helpers {
    public static class MessageExtensions {
        public static Task<IUserMessage> Reply(this IMessage msg, string content) {
            return msg.Channel.SendMessageSafe($"{msg.Author.Mention} {content}");
        }

        public static Task<IUserMessage[]> SendToAll(this IEnumerable<IMessageChannel> channels, string content) => channels.SendToAll(content, null, null, false);
        public static Task<IUserMessage[]> SendToAll(this IEnumerable<IMessageChannel> channels, string content, Embed embed) => channels.SendToAll(content, embed, null, false);
        public static Task<IUserMessage[]> SendToAll(this IEnumerable<IMessageChannel> channels, string content, Embed embed, RequestOptions options) => channels.SendToAll(content, embed, options, false);
        public static Task<IUserMessage[]> SendToAll(this IEnumerable<IMessageChannel> channels, string content, Embed embed, RequestOptions options, bool isTTS) {
            return Task.WhenAll(channels.Select(channel => channel.SendMessageSafe(content, embed, options, isTTS)));
        }

        public static Task<IUserMessage> SendMessageSafe(this IMessageChannel channel, string content) => channel.SendMessageSafe(content, null, null, false);
        public static Task<IUserMessage> SendMessageSafe(this IMessageChannel channel, string content, Embed embed) => channel.SendMessageSafe(content, embed, null, false);
        public static Task<IUserMessage> SendMessageSafe(this IMessageChannel channel, string content, Embed embed, RequestOptions options) => channel.SendMessageSafe(content, embed, options, false);
        public static async Task<IUserMessage> SendMessageSafe(this IMessageChannel channel, string content, Embed embed, RequestOptions options, bool isTTS) {
            // Make sure the bot is in this channel
            IUser user = await channel.GetUserAsync(Bot.Instance.Client.CurrentUser.Id, options: options).ConfigureAwait(false);

            // Send the message
            try {
                return user == null ? null : await channel.SendMessageAsync(content, isTTS, embed, options).ConfigureAwait(false);
            } catch (HttpException ex) {
                throw new UserMessageException(content, embed, options, ex, isTTS);
            }
        }

        public static IGuild GetGuild(this IMessage msg) => msg.Channel.GetGuild();

        public static IGuild GetGuild(this IChannel channel) {
            return channel is IGuildChannel guildChannel ? guildChannel.Guild : null;
        }

        public static Task<IMessageChannel> GetMessageChannel(this IDiscordClient client, ulong id) => client.GetMessageChannel(id, null, CacheMode.AllowDownload);
        public static Task<IMessageChannel> GetMessageChannel(this IDiscordClient client, ulong id, RequestOptions options) => client.GetMessageChannel(id, options, CacheMode.AllowDownload);
        public static Task<IMessageChannel> GetMessageChannel(this IDiscordClient client, ulong id, RequestOptions options, CacheMode mode) {
            return client.GetChannelAsync(id, mode, options).ContinueWith(task => task.Result as IMessageChannel);
        }

        public static string GetPrefix(this IMessage msg) => msg.Channel.GetGuild().GetPrefix();
        public static string GetPrefix(this IGuild guild) => Command.GetPrefix(guild);

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

        public class UserMessageException : Exception {
            public string Content { get; }
            public bool IsTTS { get; }
            public Embed Embed { get; }
            public RequestOptions Options { get; }

            public UserMessageException(string content) : this(content, null) { }
            public UserMessageException(string content, Embed embed) : this(content, embed, null) { }
            public UserMessageException(string content, Embed embed, Exception innerException) : this(content, embed, innerException, null) { }
            public UserMessageException(string content, Embed embed, Exception innerException, RequestOptions options) : this(content, embed, innerException, options, false) { }
            public UserMessageException(string content, Embed embed, Exception innerException, RequestOptions options, bool isTTS) : base($"A message failed to send: {content}", innerException) {
                this.Content = content;
                this.IsTTS = isTTS;
                this.Embed = embed;
                this.Options = options;
            }
        }
    }
}
