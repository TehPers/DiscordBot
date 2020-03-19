using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.Sets
{
    public class RedisUnlockedSetResource<T> : RedisSetResource<T>, IUnlockedSetResource<T>
    {
        public RedisUnlockedSetResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer) : base(dbFactory, resourceKey, serializer)
        {
        }

        public async Task<ILockedSetResource<T>> Reserve(TimeSpan expiry)
        {
            var resourceLock = await RedisResourceLock.Acquire(this.DbFactory, this.ResourceKey, expiry).ConfigureAwait(false);
            return new RedisLockedSetResource<T>(this.DbFactory, this.ResourceKey, this.Serializer, resourceLock);
        }
    }
}