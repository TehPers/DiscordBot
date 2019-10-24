using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace BotV2.Services.Data
{
    public class ValueReservation<T> : IValueReservation<T>
    {
        private readonly IDatabaseAsync _db;
        private readonly string _resourceKey;
        private readonly string _lockKey;
        private readonly Guid _instanceId;
        private readonly JsonSerializer _serializer;
        private int _disposed;
        private readonly TimeSpan _operationExtensionTime;

        public ValueReservation(IDatabaseAsync db, string resourceKey, string lockKey, Guid instanceId, JsonSerializer serializer)
        {
            this._db = db ?? throw new ArgumentNullException(nameof(db));
            this._resourceKey = resourceKey ?? throw new ArgumentNullException(nameof(resourceKey));
            this._lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
            this._instanceId = instanceId;
            this._serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this._disposed = 0;
            this._operationExtensionTime = TimeSpan.FromSeconds(0.1);
        }

        public async Task<Option<T>> Set(T value)
        {
            if (!await this.ExtendReservation())
            {
                throw new InvalidOperationException("The reservation on the resource has timed out");
            }

            var result = await this._db.StringGetSetAsync(this._resourceKey, this.Serialize(value));
            return result.HasValue ? new Option<T>(this.Deserialize(result)) : default;
        }

        public async Task<Option<T>> Get()
        {
            if (!await this.ExtendReservation())
            {
                throw new InvalidOperationException("The reservation on the resource has timed out");
            }

            var result = await this._db.StringGetAsync(this._resourceKey);
            return result.HasValue ? new Option<T>(this.Deserialize(result)) : default;
        }

        public async Task<bool> Delete()
        {
            if (!await this.ExtendReservation())
            {
                throw new InvalidOperationException("The reservation on the resource has timed out");
            }

            return await this._db.KeyDeleteAsync(this._resourceKey);
        }

        private Task<bool> ExtendReservation()
        {
            return this.ExtendReservation(this._operationExtensionTime);
        }

        public Task<bool> ExtendReservation(TimeSpan addedTime)
        {
            return this._db.LockExtendAsync(this._lockKey, this._instanceId.ToString(), addedTime);
        }

        private string Serialize(T value)
        {
            using var writer = new StringWriter();
            using var jsonWriter = new JsonTextWriter(writer);
            this._serializer.Serialize(jsonWriter, value);
            return writer.ToString();
        }

        private T Deserialize(string value)
        {
            using var reader = new StringReader(value);
            using var jsonReader = new JsonTextReader(reader);
            return this._serializer.Deserialize<T>(jsonReader);
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