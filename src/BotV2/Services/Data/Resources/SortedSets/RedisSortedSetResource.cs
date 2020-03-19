using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.SortedSets
{
    public abstract class RedisSortedSetResource<T> : RedisResource, ISortedSetResource<T>
        where T : IScored
    {
        protected JsonSerializer Serializer { get; set; }

        protected RedisSortedSetResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer)
            : base(dbFactory, resourceKey)
        {
            this.Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            foreach (var item in await db.SortedSetRangeByRankAsync(this.ResourceKey).ConfigureAwait(false))
            {
                yield return this.Serializer.FromString<T>(item);
            }
        }

        public virtual async Task<bool> ContainsAsync([NotNull] T item)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return db.SortedSetScoreAsync(this.ResourceKey, this.Serializer.ToString(item)) != null;
        }

        public virtual async Task<long> CountAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SortedSetLengthAsync(this.ResourceKey).ConfigureAwait(false);
        }

        Task IAsyncCollection<T>.AddAsync([NotNull] T item)
        {
            return this.AddAsync(item);
        }

        public virtual async Task<bool> AddAsync([NotNull] T item)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SortedSetAddAsync(this.ResourceKey, this.Serializer.ToString(item), item!.Score).ConfigureAwait(false);
        }

        public virtual async Task<bool> RemoveAsync([NotNull] T item)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SortedSetRemoveAsync(this.ResourceKey, this.Serializer.ToString(item)).ConfigureAwait(false);
        }

        public virtual async Task<bool> ClearAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.KeyDeleteAsync(this.ResourceKey).ConfigureAwait(false);
        }

        public virtual async Task<Option<T>> TryPeekAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            var response = await db.SortedSetRangeByRankAsync(this.ResourceKey, 0, 1).ConfigureAwait(false);
            return response.Any() ? new Option<T>(this.Serializer.FromString<T>(response[0])) : default;
        }

        public virtual async Task<Option<T>> TryPopAsync()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.SortedSetPopAsync(this.ResourceKey).ConfigureAwait(false) is { } result ? new Option<T>(this.Serializer.FromString<T>(result.Element)) : default;
        }
    }
}