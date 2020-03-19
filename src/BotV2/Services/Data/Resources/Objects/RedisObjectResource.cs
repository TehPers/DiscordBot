using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data.Resources.Objects
{
    public class RedisObjectResource<T> : RedisResource, IObjectResource<T>
    {
        protected JsonSerializer Serializer { get; }

        public RedisObjectResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer) : base(dbFactory, resourceKey)
        {
            this.Serializer = serializer;
        }

        public virtual async Task<Option<T>> Get()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            var result = await db.StringGetAsync(this.ResourceKey).ConfigureAwait(false);
            return result.HasValue ? new Option<T>(this.Serializer.FromString<T>(result)) : default;
        }

        public async Task<Option<T>> Set(T value)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            var result = await db.StringGetSetAsync(this.ResourceKey, this.Serializer.ToString(value)).ConfigureAwait(false);
            return result.HasValue ? new Option<T>(this.Serializer.FromString<T>(result)) : default;
        }

        public async Task<bool> TrySet(T value)
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.StringSetAsync(this.ResourceKey, this.Serializer.ToString(value), when: When.NotExists).ConfigureAwait(false);
        }

        public async Task<bool> Delete()
        {
            var db = await this.DbFactory.GetDatabase().ConfigureAwait(false);
            return await db.KeyDeleteAsync(this.ResourceKey).ConfigureAwait(false);
        }
    }
}