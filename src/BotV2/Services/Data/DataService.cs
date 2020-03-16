using System;
using BotV2.Services.Data.Database;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;

namespace BotV2.Services.Data
{
    public class DataService : IDataService
    {
        private readonly IDatabaseFactory _dbFactory;
        private readonly JsonSerializer _serializer;

        public DataService(IDatabaseFactory dbFactory, JsonSerializer serializer)
        {
            this._dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            this._serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public IKeyValueDataStore GetGlobalStore()
        {
            return new RedisDataStore(this._dbFactory, this._serializer, ":global");
        }

        public ICommandDataStore GetCommandStore(Command command)
        {
            return new CommandDataStore(this._dbFactory, this._serializer, $":commands:{command.QualifiedName}");
        }

        public IGuildDataStore GetGuildStore(ulong id)
        {
            return new GuildDataStore(this._dbFactory, this._serializer, $":guilds:{id}");
        }

        public IKeyValueDataStore GetUserStore(ulong id)
        {
            return new RedisDataStore(this._dbFactory, this._serializer, $":users:{id}");
        }
    }
}
