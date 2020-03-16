using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public class RedisUnlockedDelayedTaskQueueResource<T> : RedisDelayedTaskQueueResource<T>, IUnlockedDelayedTaskQueueResource<T>
    {
        public RedisUnlockedDelayedTaskQueueResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer) : base(dbFactory, resourceKey, serializer)
        {
        }

        public virtual async Task<ILockedDelayedTaskQueueResource<T>> Reserve(TimeSpan expiry)
        {
            var resourceLock = await RedisResourceLock.Acquire(this.DbFactory, this.ResourceKey, expiry);
            return new RedisLockedDelayedTaskQueueResource<T>(this.DbFactory, this.ResourceKey, this.Serializer, resourceLock);
        }
    }
}