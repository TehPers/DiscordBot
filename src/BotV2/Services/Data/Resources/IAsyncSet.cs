using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources
{
    public interface IAsyncSet<T> : IAsyncCollection<T>
    {
        new Task<bool> AddAsync(T item);
    }
}