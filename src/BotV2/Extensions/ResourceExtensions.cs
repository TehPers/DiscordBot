﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Data.Resources.DelayedTaskQueues;
using BotV2.Services.Data.Resources.Objects;
using BotV2.Services.Data.Resources.Sets;
using BotV2.Services.Data.Resources.SortedSets;

namespace BotV2.Extensions
{
    public static class ResourceExtensions
    {
        public static async Task<T> PopWait<T>(this IUnlockedDelayedTaskQueueResource<T> resource, int pollMilliseconds = 100)
        {
            _ = resource ?? throw new ArgumentNullException(nameof(resource));

            while (true)
            {
                await using (var lockedResource = await resource.Reserve(TimeSpan.FromSeconds(10)))
                {
                    if ((await lockedResource.TryPopAsync()).TryGetValue(out var popped))
                    {
                        return popped;
                    }
                }

                await Task.Delay(pollMilliseconds);
            }
        }

        public static Task<T> GetOrDefault<T>(this IObjectResource<T> resource)
        {
            // TODO: hopefully an update to C# will allow nullable generics at some point
            return resource.GetOrDefault(() => default!);
        }

        public static async Task<T> GetOrDefault<T>(this IObjectResource<T> resource, Func<T> defaultFactory)
        {
            _ = defaultFactory ?? throw new ArgumentNullException(nameof(defaultFactory));
            _ = resource ?? throw new ArgumentNullException(nameof(resource));

            var result = await resource.Get();
            return result.TryGetValue(out var value) ? value : defaultFactory();
        }

        public static async IAsyncEnumerable<T> PopAll<T>(this ISetResource<T> set, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            _ = set ?? throw new ArgumentNullException(nameof(set));

            while ((await set.TryPopAsync()).TryGetValue(out var value))
            {
                yield return value;
            }
        }

        public static async IAsyncEnumerable<T> PopAll<T>(this ISortedSetResource<T> set, [EnumeratorCancellation] CancellationToken cancellation = default)
            where T : IScored
        {
            _ = set ?? throw new ArgumentNullException(nameof(set));

            while ((await set.TryPopAsync()).TryGetValue(out var value))
            {
                yield return value;
            }
        }

        public static IAsyncEnumerable<T> PopAvailable<T>(this IUnlockedDelayedTaskQueueResource<T> queue, CancellationToken cancellation = default)
        {
            return queue.PopAvailable(TimeSpan.FromSeconds(5), cancellation);
        }

        public static async IAsyncEnumerable<T> PopAvailable<T>(this IUnlockedDelayedTaskQueueResource<T> queue, TimeSpan lockTime, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            _ = queue ?? throw new ArgumentNullException(nameof(queue));

            while (true)
            {
                cancellation.ThrowIfCancellationRequested();

                Option<T> top;
                await using (var lockedQueue = await queue.Reserve(TimeSpan.FromSeconds(5)))
                {
                    top = await lockedQueue.TryPopAsync();
                }

                if (!top.TryGetValue(out var value))
                {
                    yield break;
                }

                yield return value;
            }
        }
    }
}