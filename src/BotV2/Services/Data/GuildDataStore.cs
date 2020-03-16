using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data
{
    public class GuildDataStore : RedisDataStore, IGuildDataStore
    {
        public GuildDataStore(IDatabaseFactory dbFactory, JsonSerializer serializer, string rootKey) : base(dbFactory, serializer, rootKey) { }

        public IChannelDataStore GetChannelStore(ulong id)
        {
            return new ChannelDataStore(this.DbFactory, this.Serializer, $"{this.RootKey}:channels:{id}");
        }

        public IKeyValueDataStore GetUserStore(ulong id)
        {
            return new RedisDataStore(this.DbFactory, this.Serializer, $"{this.RootKey}:users:{id}");
        }
    }
}