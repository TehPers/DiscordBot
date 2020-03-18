using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BotV2.Extensions
{
    public static class AsyncExtensions
    {
        public static ConfiguredAsyncDisposable ConfigureAwait<T>(this T obj, bool continueOnCapturedContext, out T value)
            where T : IAsyncDisposable
        {
            value = obj;
            return obj.ConfigureAwait(continueOnCapturedContext);
        }
    }
}