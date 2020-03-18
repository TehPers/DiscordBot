using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data.Resources.Sets
{
    public abstract class RedisSetResource<T> : RedisResource, ISetResource<T>
    {
        protected JsonSerializer Serializer { get; }

        protected RedisSetResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer)
            : base(dbFactory, resourceKey)
        {
            this.Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = new CancellationToken())
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            foreach (var item in await db.SetMembersAsync(this.ResourceKey).ConfigureAwait(false))
            {
                yield return this.Serializer.FromString<T>(item);
                cancellation.ThrowIfCancellationRequested();
            }
        }

        public virtual async Task<bool> ContainsAsync(T item)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SetContainsAsync(this.ResourceKey, this.Serializer.ToString(item)).ConfigureAwait(false);
        }

        public virtual async Task<long> CountAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SetLengthAsync(this.ResourceKey).ConfigureAwait(false);
        }

        Task IAsyncCollection<T>.AddAsync(T item)
        {
            return this.AddAsync(item);
        }

        public async Task<Option<T>> TryPopAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SetPopAsync(this.ResourceKey).ConfigureAwait(false) is {HasValue: true} value ? new Option<T>(this.Serializer.FromString<T>(value)) : default;
        }

        public virtual async Task<bool> AddAsync(T item)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SetAddAsync(this.ResourceKey, this.Serializer.ToString(item)).ConfigureAwait(false);
        }

        public virtual async Task<bool> RemoveAsync(T item)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SetRemoveAsync(this.ResourceKey, this.Serializer.ToString(item)).ConfigureAwait(false);
        }

        public virtual async Task<bool> ClearAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.KeyDeleteAsync(this.ResourceKey).ConfigureAwait(false);
        }
    }
}