using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;
using StackExchange.Redis;

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
            var db = await this.DbFactory.GetDatabase();
            foreach (var item in await db.SortedSetRangeByRankAsync(this.ResourceKey))
            {
                yield return this.Serializer.FromString<T>(item);
            }
        }

        public virtual async Task<bool> ContainsAsync(T item)
        {
            var db = await this.DbFactory.GetDatabase();
            return db.SortedSetScoreAsync(this.ResourceKey, this.Serializer.ToString(item)) != null;
        }

        public virtual async Task<long> CountAsync()
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.SortedSetLengthAsync(this.ResourceKey);
        }

        Task IAsyncCollection<T>.AddAsync(T item)
        {
            return this.AddAsync(item);
        }

        public virtual async Task<bool> AddAsync(T item)
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.SortedSetAddAsync(this.ResourceKey, this.Serializer.ToString(item), item.Score);
        }

        public virtual async Task<bool> RemoveAsync(T item)
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.SortedSetRemoveAsync(this.ResourceKey, this.Serializer.ToString(item));
        }

        public virtual async Task<bool> ClearAsync()
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.KeyDeleteAsync(this.ResourceKey);
        }

        public virtual async Task<Option<T>> TryPeekAsync()
        {
            var db = await this.DbFactory.GetDatabase();
            var response = await db.SortedSetRangeByRankAsync(this.ResourceKey, 0, 1);
            return response.Any() ? new Option<T>(this.Serializer.FromString<T>(response[0])) : default;
        }

        public virtual async Task<Option<T>> TryPopAsync()
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.SortedSetPopAsync(this.ResourceKey) is { } result ? new Option<T>(this.Serializer.FromString<T>(result.Element)) : default;
        }
    }
}