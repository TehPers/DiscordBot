using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Models.Data;
using BotV2.Services.Data.Database;
using BotV2.Services.Data.Resources.SortedSets;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public abstract class RedisDelayedTaskQueueResource<T> : RedisSortedSetResource<DelayedTaskQueueItem<T>>, IDelayedTaskQueueResource<T>
    {
        protected RedisDelayedTaskQueueResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer)
            : base(dbFactory, resourceKey, serializer)
        {
        }

        public new async Task<Option<T>> TryPeekAsync()
        {
            var first = await base.TryPeekAsync();

            var now = DateTimeOffset.UtcNow;
            return first.Where(item => now >= item.Availabile).Select(item => item.Value);
        }

        public async Task<bool> AddAsync(T value, DateTimeOffset availabilityTime)
        {
            return await base.AddAsync(new DelayedTaskQueueItem<T>(value, availabilityTime));
        }
    }
}