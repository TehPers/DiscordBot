using System.Threading.Tasks;
using BotV2.Models;

namespace BotV2.Services.Data.Resources.Sets
{
    public interface ISetResource<T> : IAsyncCollection<T>, IVolatileResource
    {
        new Task<bool> AddAsync(T item);

        Task<Option<T>> TryPopAsync();
    }
}