using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Database;

namespace BotV2.Services.Data.Resources
{
    public abstract class RedisResource : IVolatileResource
    {
        protected IDatabaseFactory DbFactory { get; }
        protected string ResourceKey { get; }

        protected RedisResource(IDatabaseFactory dbFactory, string resourceKey)
        {
            this.DbFactory = dbFactory;
            this.ResourceKey = resourceKey;
        }

        public virtual async Task<bool> SetExpiry(DateTimeOffset expiry)
        {
            var db = await this.DbFactory.GetDatabase();
            return await db.KeyExpireAsync(this.ResourceKey, expiry.UtcDateTime);
        }
    }
}
