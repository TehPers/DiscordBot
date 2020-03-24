using System;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Models.Data;
using BotV2.Services.Data.Resources.SortedSets;

namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public interface IDelayedTaskQueueResource<T> : ISortedSetResource<DelayedTaskQueueItem<T>>
    {
        new Task<Option<T>> TryPeekAsync();

        Task<bool> AddAsync(T value, DateTimeOffset availabilityTime);

        new Task<Option<T>> TryPopAsync();
    }
}