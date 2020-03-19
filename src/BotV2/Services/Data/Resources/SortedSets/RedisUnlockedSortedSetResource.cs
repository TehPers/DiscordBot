using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.SortedSets
{
    public class RedisUnlockedSortedSetResource<T> : RedisSortedSetResource<T>, IUnlockedSortedSetResource<T> where T : IScored
    {
        public RedisUnlockedSortedSetResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer) : base(dbFactory, resourceKey, serializer)
        {
        }

        public async Task<ILockedSortedSetResource<T>> Reserve(TimeSpan expiry)
        {
            var resourceLock = await RedisResourceLock.Acquire(this.DbFactory, this.ResourceKey, expiry).ConfigureAwait(false);
            return new RedisLockedSortedSetResource<T>(this.DbFactory, this.ResourceKey, this.Serializer, resourceLock);
        }
    }
}