using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.Sets
{
    public sealed class RedisLockedSetResource<T> : RedisSetResource<T>, ILockedSetResource<T>
    {
        private readonly IResourceLock _resourceLock;

        public RedisLockedSetResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer, IResourceLock resourceLock) : base(dbFactory, resourceKey, serializer)
        {
            this._resourceLock = resourceLock ?? throw new ArgumentNullException(nameof(resourceLock));
        }

        public ValueTask DisposeAsync()
        {
            return this._resourceLock.DisposeAsync();
        }

        public override async Task<bool> AddAsync(T item)
        {
            if (!await this.ExtendLock())
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.AddAsync(item);
        }

        public override async Task<bool> ClearAsync()
        {

            if (!await this.ExtendLock())
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.ClearAsync();
        }

        public override async Task<bool> ContainsAsync(T item)
        {
            if (!await this.ExtendLock())
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.ContainsAsync(item);
        }

        public override async Task<long> CountAsync()
        {
            if (!await this.ExtendLock())
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.CountAsync();
        }

        public override async Task<bool> RemoveAsync(T item)
        {

            if (!await this.ExtendLock())
            {
                throw new InvalidOperationException("The lock on the resource has timed out");
            }

            return await base.RemoveAsync(item);
        }

        private Task<bool> ExtendLock()
        {
            return this.ExtendLock(TimeSpan.FromSeconds(1));
        }

        public Task<bool> ExtendLock(TimeSpan addedTime)
        {
            return this._resourceLock.ExtendLock(addedTime);
        }
    }
}