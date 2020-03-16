using System;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public class RedisLockedDelayedTaskQueueResource<T> : RedisDelayedTaskQueueResource<T>, ILockedDelayedTaskQueueResource<T>
    {
        private readonly IResourceLock _resourceLock;

        public RedisLockedDelayedTaskQueueResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer, IResourceLock resourceLock) : base(dbFactory, resourceKey, serializer)
        {
            this._resourceLock = resourceLock;
        }

        public new async Task<Option<T>> TryPopAsync()
        {
            if (!await this.ExtendLock())
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            var poppedRaw = await base.TryPopAsync();
            if (!(poppedRaw.TryGetValue(out var item) && item is { }))
            {
                return default;
            }

            var now = DateTimeOffset.UtcNow;
            if (item.Availabile > now)
            {
                await this.AddAsync(item);
                return default;
            }

            return new Option<T>(item.Value);
        }

        private Task<bool> ExtendLock()
        {
            return this.ExtendLock(TimeSpan.FromSeconds(1));
        }

        public Task<bool> ExtendLock(TimeSpan addedTime)
        {
            return this._resourceLock.ExtendLock(addedTime);
        }

        public ValueTask DisposeAsync()
        {
            return this._resourceLock.DisposeAsync();
        }
    }
}