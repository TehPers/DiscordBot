using System;
using System.Net.Http;
using BotV2.BotExtensions;
using BotV2.CommandModules.Warframe;
using BotV2.Models.WarframeInfo;
using BotV2.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Warframe;

namespace BotV2.Extensions
{
    public static class WarframeInfoExtensions
    {
        public static IServiceCollection AddWarframeInfo(this IServiceCollection services, IConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddTimedMessages();
            services.AddCommand<WarframeInfoModule>();
            services.TryAddSingleton<WarframeInfoService>();
            services.TryAddBotExtension<WarframeInfoBotExtension>();
            services.AddHttpClient();
            services.Configure<WarframeInfoConfig>(config.GetSection("Warframe"));
            services.TryAddSingleton(serviceProvider =>
            {
                var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                return new WarframeClient(WarframePlatform.Pc, (uri, cancellation) =>
                {
                    var client = clientFactory.CreateClient("warframe");
                    return client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellation);
                });
            });

            return services;
        }
    }
}
