using System;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.SortedSets
{
    public class RedisLockedSortedSetResource<T> : RedisSortedSetResource<T>, ILockedSortedSetResource<T> where T : IScored
    {
        private readonly IResourceLock _resourceLock;

        public RedisLockedSortedSetResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer, IResourceLock resourceLock) : base(dbFactory, resourceKey, serializer)
        {
            this._resourceLock = resourceLock;
        }

        public async Task<T> RemoveFirstWhen(Func<T, bool> predicate, TimeSpan pollDelay)
        {
            while (true)
            {
                var result = await this.TryPopAsync().ConfigureAwait(false);
                if (result.TryGetValue(out var value) && predicate(value))
                {
                    return value;
                }

                await this.AddAsync(value).ConfigureAwait(false);
                await Task.Delay(pollDelay).ConfigureAwait(false);
            }
        }

        public override async Task<bool> AddAsync(T item)
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.AddAsync(item).ConfigureAwait(false);
        }

        public override async Task<bool> ClearAsync()
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.ClearAsync().ConfigureAwait(false);
        }

        public override async Task<bool> ContainsAsync(T item)
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.ContainsAsync(item).ConfigureAwait(false);
        }

        public override async Task<long> CountAsync()
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.CountAsync().ConfigureAwait(false);
        }

        public override async Task<bool> RemoveAsync(T item)
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.RemoveAsync(item).ConfigureAwait(false);
        }

        public override async Task<Option<T>> TryPeekAsync()
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.TryPeekAsync().ConfigureAwait(false);
        }

        public override async Task<Option<T>> TryPopAsync()
        {
            if (!await this.ExtendLock().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.TryPopAsync().ConfigureAwait(false);
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