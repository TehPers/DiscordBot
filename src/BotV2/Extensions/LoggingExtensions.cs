using System;
using Microsoft.Extensions.Logging;

namespace BotV2.Extensions
{
    public static class LoggingExtensions
    {
        public static IDisposable BeginScopeWithProperty(this ILogger logger, string key, object value)
        {
            // return logger.BeginScope(new[] { new KeyValuePair<string, object>(key, value) });
            return logger.BeginScope($"{key}: {value}");
        }
    }
}
