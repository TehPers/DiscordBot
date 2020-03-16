using System;
using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources.SortedSets
{
    public interface ILockedSortedSetResource<T> : ISortedSetResource<T>, IResourceLock
        where T : IScored
    {
        Task<T> RemoveFirstWhen(Func<T, bool> predicate, TimeSpan pollDelay);
    }
}