using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources
{
    public interface IAsyncCollection<T> : IReadOnlyAsyncCollection<T>
    {
        Task AddAsync(T item);

        Task<bool> RemoveAsync(T item);

        Task<bool> ClearAsync();
    }
}