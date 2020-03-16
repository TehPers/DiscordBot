using System.Threading.Tasks;
using StackExchange.Redis;

namespace BotV2.Services.Data.Database
{
    public interface IDatabaseFactory
    {
        Task<IDatabaseAsync> GetDatabase();
    }
}