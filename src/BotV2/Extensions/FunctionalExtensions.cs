using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BotV2.Extensions
{
    public static class FunctionalExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static T2 Forward<T1, T2>(this T1 source, Func<T1, T2> transform)
        {
            _ = transform ?? throw new ArgumentNullException(nameof(transform));

            return transform(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static void Forward<T>(this T source, Action<T> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));

            action(source);
        }
    }
}