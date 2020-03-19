using System;
using BotV2.CommandModules.FireEmblem;
using BotV2.Models.FireEmblem;
using BotV2.Services.FireEmblem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotV2.Extensions
{
    public static class FehExtensions
    {
        public static IServiceCollection AddFireEmblem(this IServiceCollection services, IConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddGoogleSheets(config.GetSection("Google"));
            services.TryAddSingleton<IFehDataProvider, FehDataProvider>();
            services.Configure<FehDataProviderConfig>(config.GetSection("FEH"));
            services.AddCommand<FehModule>();
            return services;
        }
    }
}