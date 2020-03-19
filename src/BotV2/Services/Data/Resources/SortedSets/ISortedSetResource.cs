using System.Threading.Tasks;
using BotV2.Models;

namespace BotV2.Services.Data.Resources.SortedSets
{
    public interface ISortedSetResource<T> : IAsyncSet<T>, IVolatileResource
        where T : IScored
    {
        Task<Option<T>> TryPeekAsync();

        Task<Option<T>> TryPopAsync();
    }
}
