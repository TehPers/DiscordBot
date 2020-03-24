using System;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Models.Data;
using BotV2.Services.Data.Database;
using BotV2.Services.Data.Resources.SortedSets;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public class RedisDelayedTaskQueueResource<T> : RedisSortedSetResource<DelayedTaskQueueItem<T>>, IDelayedTaskQueueResource<T>
    {
        public RedisDelayedTaskQueueResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer)
            : base(dbFactory, resourceKey, serializer)
        {
        }

        public new async Task<Option<T>> TryPeekAsync()
        {
            var first = await base.TryPeekAsync().ConfigureAwait(false);

            var now = DateTimeOffset.UtcNow;
            return first.Where(item => now >= item.Availabile).Select(item => item.Value);
        }

        public async Task<bool> AddAsync(T value, DateTimeOffset availabilityTime)
        {
            return await base.AddAsync(new DelayedTaskQueueItem<T>(value, availabilityTime)).ConfigureAwait(false);
        }

        public new async Task<Option<T>> TryPopAsync()
        {
            var poppedRaw = await base.TryPopAsync().ConfigureAwait(false);
            if (!(poppedRaw.TryGetValue(out var item) && item is { }))
            {
                return default;
            }

            var now = DateTimeOffset.UtcNow;
            if (item.Availabile > now)
            {
                await this.AddAsync(item).ConfigureAwait(false);
                return default;
            }

            return new Option<T>(item.Value);
        }
    }
}