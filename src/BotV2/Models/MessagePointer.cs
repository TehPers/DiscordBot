using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;

namespace BotV2.Models
{
    public readonly struct MessagePointer : IEquatable<MessagePointer>
    {
        [JsonProperty]
        public ulong MessageId { get; }

        [JsonProperty]
        public ulong ChannelId { get; }

        [JsonConstructor]
        public MessagePointer(ulong messageId, ulong channelId)
        {
            this.MessageId = messageId;
            this.ChannelId = channelId;
        }

        public MessagePointer(DiscordMessage message)
            : this(message.Id, message.ChannelId)
        {
        }

        public async Task<DiscordMessage?> TryGetMessage(DiscordClient client)
        {
            _ = client ?? throw new ArgumentNullException(nameof(client));

            try
            {
                if (await client.GetChannelAsync(this.ChannelId) is { } channel)
                {
                    return await channel.GetMessageAsync(this.MessageId).ConfigureAwait(false);
                }
            }
            catch (NotFoundException)
            {
            }

            return null;
        }

        public bool Equals(MessagePointer other)
        {
            return this.MessageId == other.MessageId && this.ChannelId == other.ChannelId;
        }

        public override bool Equals(object? obj)
        {
            return obj is MessagePointer other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.MessageId, this.ChannelId);
        }

        public override string ToString()
        {
            return $"{this.ChannelId}/{this.MessageId}";
        }
    }
}