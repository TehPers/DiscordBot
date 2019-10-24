using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data
{
    public class ChannelDataStore : RedisDataStore, IChannelDataStore
    {
        public ChannelDataStore(IDatabaseAsync db, JsonSerializer serializer, string rootKey) : base(db, serializer, rootKey) { }

        public IKeyValueDataStore GetUserStore(ulong id)
        {
            return new RedisDataStore(this.Db, this.Serializer, $"{this.RootKey}:users:{id}");
        }
    }
}