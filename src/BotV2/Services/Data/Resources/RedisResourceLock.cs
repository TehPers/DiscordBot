using System;
using System.Threading.Tasks;
using BotV2.Services.Data.Database;

namespace BotV2.Services.Data.Resources
{
    public sealed class RedisResourceLock : IResourceLock
    {
        private const string BaseKey = ":lock";

        private readonly IDatabaseFactory _dbFactory;
        private readonly string _lockKey;
        private readonly Guid _instanceId;

        public RedisResourceLock(IDatabaseFactory dbFactory, string lockKey, Guid instanceId)
        {
            this._dbFactory = dbFactory;
            this._lockKey = lockKey ?? throw new ArgumentNullException(nameof(lockKey));
            this._instanceId = instanceId;
        }

        public async Task<bool> ExtendLock(TimeSpan addedTime)
        {
            var db = await this._dbFactory.GetDatabase().ConfigureAwait(false);
            return await db.LockExtendAsync(this._lockKey, this._instanceId.ToString(), addedTime).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            var db = await this._dbFactory.GetDatabase().ConfigureAwait(false);
            await db.LockReleaseAsync(this._lockKey, this._instanceId.ToString()).ConfigureAwait(false);
        }

        public static string GetLockKey(string resourceKey)
        {
            return $"{RedisResourceLock.BaseKey}:{resourceKey}";
        }

        public static async Task<RedisResourceLock> Acquire(IDatabaseFactory dbFactory, string resourceKey, TimeSpan expiry)
        {
            var lockKey = RedisResourceLock.GetLockKey(resourceKey);
            var instanceId = Guid.NewGuid();
            var db = await dbFactory.GetDatabase().ConfigureAwait(false);
            while (!await db.LockTakeAsync(lockKey, instanceId.ToString(), expiry).ConfigureAwait(false))
            {
                await Task.Delay(100).ConfigureAwait(false);
                db = await dbFactory.GetDatabase().ConfigureAwait(false);
            }

            return new RedisResourceLock(dbFactory, lockKey, instanceId);
        }
    }
}