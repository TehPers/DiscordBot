using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources
{
    public interface IReadOnlyAsyncCollection<T> : IAsyncEnumerable<T>
    {
        Task<bool> ContainsAsync(T item);

        Task<long> CountAsync();
    }
}