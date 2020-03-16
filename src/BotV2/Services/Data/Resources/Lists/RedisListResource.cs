using System;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Services.Data.Database;
using Newtonsoft.Json;

namespace BotV2.Services.Data.Resources.Lists
{
    public class RedisListResource<T> : RedisResource, IListResource<T>
    {
        protected JsonSerializer Serializer { get; }

        public RedisListResource(IDatabaseFactory dbFactory, string resourceKey, JsonSerializer serializer)
            : base(dbFactory, resourceKey)
        {
            this.Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task Add(T item)
        {
            var db = await this.DbFactory.GetDatabase();
            await db.ListRightPushAsync(this.ResourceKey, this.Serializer.ToString(item));
        }
    }
}