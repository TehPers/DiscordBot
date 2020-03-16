using System;
using BotV2.Services.Data;
using BotV2.Services.Data.Database;
using BotV2.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BotV2.Extensions
{
    public static class RedisExtensions
    {
        public static void AddRedis(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IDatabaseFactory, RedisDatabaseFactory>();
            services.TryAddSingleton<IDataService, DataService>();
            services.AddJsonSerializer();
        }

        public static ILoggingBuilder AddDatabase(this ILoggingBuilder builder)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DatabaseLoggerProvider>());
            return builder;
        }
    }
}