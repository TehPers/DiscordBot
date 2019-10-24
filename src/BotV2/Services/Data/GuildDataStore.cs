using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data
{
    public class GuildDataStore : RedisDataStore, IGuildDataStore
    {
        public GuildDataStore(IDatabaseAsync db, JsonSerializer serializer, string rootKey) : base(db, serializer, rootKey) { }

        public IChannelDataStore GetChannelStore(ulong id)
        {
            return new ChannelDataStore(this.Db, this.Serializer, $"{this.RootKey}:channels:{id}");
        }

        public IKeyValueDataStore GetUserStore(ulong id)
        {
            return new RedisDataStore(this.Db, this.Serializer, $"{this.RootKey}:users:{id}");
        }
    }
}