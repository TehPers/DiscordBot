using System.Threading.Tasks;
using BotV2.Models;

namespace BotV2.Services.Data.Resources.Objects
{
    public interface IObjectResource<T> : IVolatileResource
    {
        Task<Option<T>> Get();

        Task<Option<T>> Set(T value);

        Task<bool> TrySet(T value);

        Task<bool> Delete();
    }
}