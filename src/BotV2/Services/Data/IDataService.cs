using DSharpPlus.CommandsNext;

namespace BotV2.Services.Data
{
    public interface IDataService
    {
        IKeyValueDataStore GetGlobalStore();

        ICommandDataStore GetCommandStore(Command command);

        IGuildDataStore GetGuildStore(ulong id);

        IKeyValueDataStore GetUserStore(ulong id);
    }
}