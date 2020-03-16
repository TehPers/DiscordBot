using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data
{
    public class CommandDataStore : RedisDataStore, ICommandDataStore
    {
        public CommandDataStore(IDatabaseFactory dbFactory, JsonSerializer serializer, string rootKey) : base(dbFactory, serializer, rootKey) { }

        public IGuildDataStore GetGuildStore(ulong id)
        {
            return new GuildDataStore(this.DbFactory, this.Serializer, $"{this.RootKey}:guilds:{id}");
        }

        public IKeyValueDataStore GetUserStore(ulong id)
        {
            return new RedisDataStore(this.DbFactory, this.Serializer, $"{this.RootKey}:users:{id}");
        }
    }
}