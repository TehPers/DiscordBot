using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data.Resources.HashTables
{
    public class RedisHashTableResource<T> : RedisResource, IHashTableResource<T>
    {
        protected JsonSerializer Serializer { get; }

        public RedisHashTableResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer)
            : base(dbFactory, resourceKey)
        {
            this.Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<bool> ContainsAsync(KeyValuePair<RedisValue, T> item)
        {
            var stored = await this.TryGetAsync(item.Key);
            return stored.TryGetValue(out var value) && EqualityComparer<T>.Default.Equals(item.Value, value);
        }

        public async Task<long> CountAsync()
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.HashLengthAsync(this.ResourceKey);
        }

        public Task AddAsync(KeyValuePair<RedisValue, T> item)
        {
            return this.AddAsync(item.Key, item.Value);
        }

        public Task<bool> RemoveAsync(KeyValuePair<RedisValue, T> item)
        {
            throw new InvalidOperationException("This operation cannot be performed atomically");
        }

        public async Task<bool> ClearAsync()
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.KeyDeleteAsync(this.ResourceKey);
        }

        public async Task<bool> AddAsync(RedisValue key, T value)
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.HashSetAsync(this.ResourceKey, key, this.Serializer.ToString(value), When.NotExists);
        }

        public async Task<bool> ContainsKeyAsync(RedisValue key)
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.HashExistsAsync(this.ResourceKey, key);
        }

        public async Task SetAsync(RedisValue key, T value)
        {
            var db = await this.DbFactory.GetDatabase();
            await db.HashSetAsync(this.ResourceKey, key, this.Serializer.ToString(value));
        }

        public async Task<bool> RemoveKeyAsync(RedisValue key)
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.HashDeleteAsync(this.ResourceKey, key);
        }

        public async Task<Option<T>> TryGetAsync(RedisValue key)
        {
            var db = await this.DbFactory.GetDatabase();
            var stored = await db.HashGetAsync(this.ResourceKey, key);
            return stored == RedisValue.Null ? new Option<T>() : new Option<T>(this.Serializer.FromString<T>(stored));
        }

        public async IAsyncEnumerator<KeyValuePair<RedisValue, T>> GetAsyncEnumerator(CancellationToken cancellation = default)
        {
            var db = await this.DbFactory.GetDatabase();
            var keys = await db.HashKeysAsync(this.ResourceKey);
            foreach (var key in keys)
            {
                cancellation.ThrowIfCancellationRequested();
                var value = await db.HashGetAsync(this.ResourceKey, key);
                if (value != RedisValue.Null)
                {
                    yield return new KeyValuePair<RedisValue, T>(key, this.Serializer.FromString<T>(value));
                }
            }
        }
    }
}