using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data
{
    [SuppressMessage("ReSharper", "HeuristicUnreachableCode", Justification = "ReSharper is wrong")]
    public class RedisDataStore : IKeyValueDataStore
    {
        protected IDatabaseAsync Db { get; }
        protected JsonSerializer Serializer { get; }
        protected string RootKey { get; }

        public RedisDataStore(IDatabaseAsync db, JsonSerializer serializer, string rootKey)
        {
            this.Db = db;
            this.Serializer = serializer;
            this.RootKey = rootKey;
        }

        private string GetFullResourceKey(string subKey)
        {
            _ = subKey ?? throw new ArgumentNullException(nameof(subKey));
            return subKey == string.Empty ? this.RootKey : $"{this.RootKey}:values:{subKey}";
        }

        // private async Task<IAsyncDisposable> AcquireLock(string key, TimeSpan expiry)
        // {
        //     _ = key ?? throw new ArgumentNullException(nameof(key));
        // 
        //     var lockKey = $"/locks/{key}";
        //     var instanceId = Guid.NewGuid();
        //     while (!await this.Db.LockTakeAsync(lockKey, instanceId.ToString(), expiry)) { }
        // 
        //     return new RedisLockReservation(this.Db, lockKey, instanceId);
        // }

        public async Task<T> AddOrGet<T>(string key, Func<T> addFactory)
        {
            _ = addFactory ?? throw new ArgumentNullException(nameof(addFactory));
            _ = key ?? throw new ArgumentNullException(nameof(key));

            var fullKey = this.GetFullResourceKey(key);
            var added = new Lazy<T>(addFactory);
            var addedSerialized = new Lazy<Task<string>>(async () =>
            {
                await using var writer = new StringWriter();
                using var jsonWriter = new JsonTextWriter(writer);
                this.Serializer.Serialize(jsonWriter, added.Value);
                return writer.ToString();
            });

            // Try until success - this should only repeat if the value changes mid-operation
            while (true)
            {
                // Try to get the value
                if (await this.Db.StringGetAsync(fullKey) is { HasValue: true } curValue)
                {
                    using var reader = new StringReader(curValue);
                    using var jsonReader = new JsonTextReader(reader);
                    return this.Serializer.Deserialize<T>(jsonReader);
                }

                // Set the value if it doesn't exist
                var value = await addedSerialized.Value;
                if (await this.Db.StringSetAsync(fullKey, value, when: When.NotExists))
                {
                    return added.Value;
                }

                // Database was modified, so restart this operation
            }
        }

        public async Task<T> AddOrUpdate<T>(string key, Func<T> addFactory, Func<T, T> updateFactory)
        {
            _ = updateFactory ?? throw new ArgumentNullException(nameof(updateFactory));
            _ = addFactory ?? throw new ArgumentNullException(nameof(addFactory));
            _ = key ?? throw new ArgumentNullException(nameof(key));

            var fullKey = this.GetFullResourceKey(key);
            var added = new Lazy<T>(addFactory);
            var addedSerialized = new Lazy<Task<string>>(async () =>
            {
                await using var writer = new StringWriter();
                using var jsonWriter = new JsonTextWriter(writer);
                this.Serializer.Serialize(jsonWriter, added.Value);
                return writer.ToString();
            });

            // Try until success - this should only repeat if the value changes mid-operation
            while (true)
            {
                // Try to get the value
                if (await this.Db.StringGetAsync(fullKey) is { HasValue: true } prevSerialized)
                {
                    await using (await this.Reserve<T>(key, TimeSpan.FromSeconds(30)))
                    {
                        // Verify the string's value hasn't changed
                        if (await this.Db.StringGetAsync(fullKey) is { HasValue: true } != prevSerialized)
                        {
                            continue;
                        }

                        // Deserialize the previous value
                        using var prevValueReader = new StringReader(prevSerialized);
                        using var prevValueJsonReader = new JsonTextReader(prevValueReader);
                        var prevValue = this.Serializer.Deserialize<T>(prevValueJsonReader);

                        // Create and serialize the new value
                        var newValue = updateFactory(prevValue);
                        await using var newValueWriter = new StringWriter();
                        using var newValueJsonWriter = new JsonTextWriter(newValueWriter);
                        this.Serializer.Serialize(newValueJsonWriter, newValue);

                        // Set the value
                        await this.Db.StringSetAsync(fullKey, newValueWriter.ToString(), flags: CommandFlags.FireAndForget);
                        return newValue;
                    }
                }

                // Set the value if it doesn't exist
                var value = await addedSerialized.Value;
                if (await this.Db.StringSetAsync(fullKey, value, when: When.NotExists))
                {
                    return added.Value;
                }
            }
        }

        public async Task<(bool success, T value)> TryGet<T>(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));

            var fullKey = this.GetFullResourceKey(key);
            if (!(await this.Db.StringGetAsync(fullKey) is { HasValue: true } curRaw))
            {
                return (false, default);
            }

            using var reader = new StringReader(curRaw);
            using var jsonReader = new JsonTextReader(reader);
            var cur = this.Serializer.Deserialize<T>(jsonReader);
            return (true, cur);
        }

        public async Task Set<T>(string key, T value)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));

            var fullKey = this.GetFullResourceKey(key);
            await using var writer = new StringWriter();
            using var jsonWriter = new JsonTextWriter(writer);
            this.Serializer.Serialize(jsonWriter, value);
            await this.Db.StringSetAsync(fullKey, writer.ToString(), flags: CommandFlags.FireAndForget);
        }

        public async Task<IValueReservation<T>> Reserve<T>(string key, TimeSpan expiry)
        {
            var resourceKey = this.GetFullResourceKey(key);
            var lockKey = $"/locks/{key}";
            var instanceId = Guid.NewGuid();

            while (!await this.Db.LockTakeAsync(lockKey, instanceId.ToString(), expiry))
            {
                await Task.Delay(100);
            }

            return new ValueReservation<T>(this.Db, resourceKey, lockKey, instanceId, this.Serializer);
        }

        private class RedisLockReservation : IAsyncDisposable
        {
            private readonly IDatabaseAsync _db;
            private readonly string _lockKey;
            private readonly Guid _instanceId;
            private int _disposed;

            public RedisLockReservation(IDatabaseAsync db, string lockKey, Guid instanceId)
            {
                this._db = db ?? throw new ArgumentNullException(nameof(db));
                this._lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
                this._instanceId = instanceId;
                this._disposed = 1;
            }

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref this._disposed, 1) == 0)
                {
                    if (!await this._db.LockReleaseAsync(this._lockKey, this._instanceId.ToString()))
                    {
                        throw new TimeoutException("The lock timed out before being released");
                    }
                }
            }
        }
    }
}