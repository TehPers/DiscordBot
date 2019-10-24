using System.Threading.Tasks;
using DSharpPlus.CommandsNext;

namespace BotV2.Services.Data
{
    public interface IDataService
    {
        Task<IKeyValueDataStore> GetGlobalStore();

        Task<ICommandDataStore> GetCommandStore(Command command);

        Task<IGuildDataStore> GetGuildStore(ulong id);

        Task<IKeyValueDataStore> GetUserStore(ulong id);
    }
}