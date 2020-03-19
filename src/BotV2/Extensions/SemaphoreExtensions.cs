using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotV2.Extensions
{
    public static class SemaphoreExtensions
    {
        public static async ValueTask<IDisposable> AutoLockAsync(this SemaphoreSlim semaphore, CancellationToken cancellation = default)
        {
            _ = semaphore ?? throw new ArgumentNullException(nameof(semaphore));

            await semaphore.WaitAsync(cancellation).ConfigureAwait(false);
            return new Lock(() => semaphore.Release());
        }

        private sealed class Lock : IDisposable
        {
            private readonly Action _release;

            public Lock(Action release)
            {
                this._release = release;
            }

            public void Dispose()
            {
                this._release();
            }
        }
    }
}