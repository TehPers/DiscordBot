using System;
using BotV2.Services.Data;
using BotV2.Services.Data.Connection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotV2.Extensions
{
    public static class RedisExtensions
    {
        public static void AddRedis(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IDatabaseFactory, RedisDatabaseFactory>();
            services.TryAddSingleton<IDataService, DataService>();
        }
    }
}
