using System;
using System.Diagnostics.CodeAnalysis;
using BotV2.Services.Data.Database;
using BotV2.Services.Data.Resources;
using BotV2.Services.Data.Resources.DelayedTaskQueues;
using BotV2.Services.Data.Resources.HashTables;
using BotV2.Services.Data.Resources.Lists;
using BotV2.Services.Data.Resources.Objects;
using BotV2.Services.Data.Resources.Sets;
using BotV2.Services.Data.Resources.SortedSets;
using Newtonsoft.Json;

namespace BotV2.Services.Data
{
    [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "ReSharper is wrong")]
    public class RedisDataStore : IKeyValueDataStore
    {
        protected IDatabaseFactory DbFactory { get; }
        protected JsonSerializer Serializer { get; }
        protected string RootKey { get; }

        public RedisDataStore(IDatabaseFactory dbFactory, JsonSerializer serializer, string rootKey)
        {
            this.DbFactory = dbFactory;
            this.Serializer = serializer;
            this.RootKey = rootKey;
        }

        private string GetFullResourceKey(string subKey)
        {
            _ = subKey ?? throw new ArgumentNullException(nameof(subKey));
            return subKey == string.Empty ? this.RootKey : $"{this.RootKey}:values:{subKey}";
        }

        public IObjectResource<T> GetObjectResource<T>(string key)
        {
            return new RedisObjectResource<T>(this.DbFactory, this.GetFullResourceKey(key), this.Serializer);
        }

        public IUnlockedDelayedTaskQueueResource<T> GetDelayedTaskQueueResource<T>(string key)
        {
            return new RedisUnlockedDelayedTaskQueueResource<T>(this.DbFactory, this.GetFullResourceKey(key), this.Serializer);
        }

        public IListResource<T> GetListResource<T>(string key)
        {
            return new RedisListResource<T>(this.DbFactory, this.GetFullResourceKey(key), this.Serializer);
        }

        public IUnlockedSetResource<T> GetSetResource<T>(string key)
        {
            return new RedisUnlockedSetResource<T>(this.DbFactory, this.GetFullResourceKey(key), this.Serializer);
        }

        public IUnlockedSortedSetResource<T> GetSortedSetResource<T>(string key) where T : IScored
        {
            return new RedisUnlockedSortedSetResource<T>(this.DbFactory, this.GetFullResourceKey(key), this.Serializer);
        }

        public IHashTableResource<T> GetTableResource<T>(string key)
        {
            return new RedisHashTableResource<T>(this.DbFactory, this.GetFullResourceKey(key), this.Serializer);
        }
    }
}