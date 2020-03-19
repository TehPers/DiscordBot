using StackExchange.Redis;

namespace BotV2.Services.Data.Resources.HashTables
{
    public interface IHashTableResource<T> : IAsyncDictionary<RedisValue, T>, IVolatileResource
    {
    }
}