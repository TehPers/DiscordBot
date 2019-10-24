using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data
{
    public class CommandDataStore : RedisDataStore, ICommandDataStore
    {
        public CommandDataStore(IDatabaseAsync db, JsonSerializer serializer, string rootKey) : base(db, serializer, rootKey) { }

        public IGuildDataStore GetGuildStore(ulong id)
        {
            return new GuildDataStore(this.Db, this.Serializer, $"{this.RootKey}:guilds:{id}");
        }

        public IKeyValueDataStore GetUserStore(ulong id)
        {
            return new RedisDataStore(this.Db, this.Serializer, $"{this.RootKey}:users:{id}");
        }
    }
}