using System.Threading.Tasks;
using StackExchange.Redis;

namespace BotV2.Services.Data.Connection
{
    public interface IDatabaseFactory
    {
        Task<IDatabaseAsync> GetDatabase();
    }
}