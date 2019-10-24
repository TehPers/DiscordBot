using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotV2.Extensions
{
    public static class LockingExtensions
    {
        public static async Task<IDisposable> AcquireAsync(this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new SemaphoreSlimReservation(semaphore);
        }

        public static async Task<IDisposable> AcquireAsync(this SemaphoreSlim semaphore, CancellationToken cancellation)
        {
            await semaphore.WaitAsync(cancellation);
            return new SemaphoreSlimReservation(semaphore);
        }

        private class SemaphoreSlimReservation : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreSlimReservation(SemaphoreSlim semaphore)
            {
                this._semaphore = semaphore;
            }

            public void Dispose()
            {
                this._semaphore.Release();
            }
        }
    }
}
