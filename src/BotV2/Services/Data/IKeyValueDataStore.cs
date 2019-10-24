using System;
using System.Threading.Tasks;

namespace BotV2.Services.Data
{
    public interface IKeyValueDataStore
    {
        Task<T> AddOrGet<T>(string key, Func<T> addFactory);

        Task<T> AddOrUpdate<T>(string key, Func<T> addFactory, Func<T, T> updateFactory);

        Task<(bool success, T value)> TryGet<T>(string key);

        Task Set<T>(string key, T value);
    }
}