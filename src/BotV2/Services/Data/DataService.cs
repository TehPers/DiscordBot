using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Connection;
using DSharpPlus.CommandsNext;
using Newtonsoft.Json;

namespace BotV2.Services.Data
{
    public class DataService : IDataService
    {
        private readonly IDatabaseFactory _dbFactory;
        private readonly JsonSerializer _serializer;

        public DataService(IDatabaseFactory dbFactory)
        {
            this._dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            this._serializer = JsonSerializer.CreateDefault();
        }

        public async Task<IKeyValueDataStore> GetGlobalStore()
        {
            var db = await this._dbFactory.GetDatabase();
            return new RedisDataStore(db, this._serializer, ":global");
        }

        public async Task<ICommandDataStore> GetCommandStore(Command command)
        {
            var db = await this._dbFactory.GetDatabase();
            return new CommandDataStore(db, this._serializer, $":commands:{command.QualifiedName}");
        }

        public async Task<IGuildDataStore> GetGuildStore(ulong id)
        {
            var db = await this._dbFactory.GetDatabase();
            return new GuildDataStore(db, this._serializer, $":guilds:{id}");
        }

        public async Task<IKeyValueDataStore> GetUserStore(ulong id)
        {
            var db = await this._dbFactory.GetDatabase();
            return new RedisDataStore(db, this._serializer, $":users:{id}");
        }
    }
}
