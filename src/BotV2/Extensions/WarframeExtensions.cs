﻿using System;
using System.Net.Http;
using System.Text;
using BotV2.BotExtensions;
using BotV2.CommandModules.Warframe;
using BotV2.Models.WarframeInfo;
using BotV2.Services.WarframeInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
            services.TryAddSingleton<IWarframeClient>(serviceProvider =>
            {
                var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var logger = serviceProvider.GetRequiredService<ILogger<WarframeClient>>();
                return new WarframeClient(WarframePlatform.Pc, (uri, cancellation) =>
                {
                    logger.LogTrace($"Request: GET {uri}");
                    var client = clientFactory.CreateClient("warframe");
                    return client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellation);
                });
            });

            // Cycles
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Singleton<IWarframeCycle, WarframeEarthCycle>(),
                ServiceDescriptor.Singleton<IWarframeCycle, WarframeCetusCycle>(),
                ServiceDescriptor.Singleton<IWarframeCycle, WarframeVallisCycle>(),
                ServiceDescriptor.Singleton<IWarframeCycle, WarframeCambionCycle>()
            });

            return services;
        }

        public static string FormatWarframeTime(this TimeSpan interval)
        {
            var result = new StringBuilder();

            if (interval.Days > 0)
            {
                result.Append($"{interval.Days}d ");
            }

            if (interval.Hours > 0)
            {
                result.Append($"{interval.Hours}h ");
            }

            if (interval.Minutes > 0 || interval.TotalMinutes < 1)
            {
                result.Append($"{Math.Ceiling(interval.TotalMinutes % 60)}m");
            }

            return result.ToString();
        }
    }
}