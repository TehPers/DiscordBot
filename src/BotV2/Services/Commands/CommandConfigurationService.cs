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
            var dataStore = this._dataService.GetGuildStore(guildId);
            var resource = dataStore.GetObjectResource<bool>($"commands:{command.QualifiedName}:enabled");
            if ((await resource.Get().ConfigureAwait(false)).TryGetValue(out var enabled))
            {
                return enabled;
            }

            return true;
        }

        public async Task SetCommandEnabled(Command command, ulong guildId, bool enabled)
        {
            var dataStore = this._dataService.GetGuildStore(guildId);
            var resource = dataStore.GetObjectResource<bool>($"commands:{command.QualifiedName}:enabled");
            await resource.Set(enabled).ConfigureAwait(false);
        }
    }
}