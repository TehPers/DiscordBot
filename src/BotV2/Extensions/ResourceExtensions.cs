using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Services.Data.Resources.DelayedTaskQueues;
using BotV2.Services.Data.Resources.Objects;
using BotV2.Services.Data.Resources.Sets;
using BotV2.Services.Data.Resources.SortedSets;

namespace BotV2.Extensions
{
    public static class ResourceExtensions
    {
        public static async Task<T> PopWait<T>(this IDelayedTaskQueueResource<T> resource, int pollMilliseconds = 100)
        {
            _ = resource ?? throw new ArgumentNullException(nameof(resource));

            T popped;
            while (!(await resource.TryPopAsync().ConfigureAwait(false)).TryGetValue(out popped))
            {
                await Task.Delay(pollMilliseconds).ConfigureAwait(false);
            }

            return popped;
        }

        public static Task<T> GetOrDefault<T>(this IObjectResource<T> resource)
        {
            return resource.GetOrDefault(() => default!);
        }

        public static async Task<T> GetOrDefault<T>(this IObjectResource<T> resource, Func<T> defaultFactory)
        {
            _ = defaultFactory ?? throw new ArgumentNullException(nameof(defaultFactory));
            _ = resource ?? throw new ArgumentNullException(nameof(resource));

            var result = await resource.Get().ConfigureAwait(false);
            return result.TryGetValue(out var value) ? value : defaultFactory();
        }

        public static async IAsyncEnumerable<T> PopAll<T>(this ISetResource<T> set, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            _ = set ?? throw new ArgumentNullException(nameof(set));

            cancellation.ThrowIfCancellationRequested();
            while ((await set.TryPopAsync().ConfigureAwait(false)).TryGetValue(out var value))
            {
                yield return value;
                cancellation.ThrowIfCancellationRequested();
            }
        }

        public static async IAsyncEnumerable<T> PopAll<T>(this ISortedSetResource<T> set, [EnumeratorCancellation] CancellationToken cancellation = default)
            where T : IScored
        {
            _ = set ?? throw new ArgumentNullException(nameof(set));

            cancellation.ThrowIfCancellationRequested();
            while ((await set.TryPopAsync().ConfigureAwait(false)).TryGetValue(out var value))
            {
                yield return value;
                cancellation.ThrowIfCancellationRequested();
            }
        }

        public static async IAsyncEnumerable<T> PopAvailable<T>(this IDelayedTaskQueueResource<T> queue, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            _ = queue ?? throw new ArgumentNullException(nameof(queue));

            cancellation.ThrowIfCancellationRequested();
            while ((await queue.TryPopAsync().ConfigureAwait(false)).TryGetValue(out var value))
            {
                yield return value;
                cancellation.ThrowIfCancellationRequested();
            }
        }
    }
}