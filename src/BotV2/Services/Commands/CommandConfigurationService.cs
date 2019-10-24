using System;
using System.Threading.Tasks;
using BotV2.Services.Data;
using DSharpPlus.CommandsNext;

namespace BotV2.Services.Commands
{
    public class CommandConfigurationService
    {
        private readonly IDataService _dataService;

        public CommandConfigurationService(IDataService dataService)
        {
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public async Task<bool> IsCommandEnabled(Command command, ulong guildId)
        {
            var dataStore = await this._dataService.GetGuildStore(guildId);
            return await dataStore.AddOrGet($"commands:{command.QualifiedName}:enabled", () => true);
        }

        public async Task SetCommandEnabled(Command command, ulong guildId, bool isEnabled)
        {
            var dataStore = await this._dataService.GetGuildStore(guildId);
            await dataStore.AddOrUpdate($"commands:{command.QualifiedName}:enabled", () => isEnabled, _ => isEnabled);
        }
    }
}