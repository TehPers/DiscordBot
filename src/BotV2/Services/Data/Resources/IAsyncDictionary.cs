using System.Collections.Generic;
using System.Threading.Tasks;
using BotV2.Models;

namespace BotV2.Services.Data.Resources
{
    public interface IAsyncDictionary<TKey, TValue> : IAsyncCollection<KeyValuePair<TKey, TValue>>
    {
        Task<bool> AddAsync(TKey key, TValue value);

        Task<bool> ContainsKeyAsync(TKey key);

        Task SetAsync(TKey key, TValue value);

        Task<bool> RemoveKeyAsync(TKey key);

        Task<Option<TValue>> TryGetAsync(TKey key);
    }
}