using System;
using System.Text.RegularExpressions;
using BotV2.Models;
using BotV2.Services;
using BotV2.Services.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BotV2.Extensions
{
    public static class DiscordServiceExtensions
    {
        private static readonly Regex CommandRegex = new Regex(@"(?<cmd>\S+)\b");

        public static IServiceCollection AddDiscordClient(this IServiceCollection services, IConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<EmbedService>();
            services.TryAddSingleton(serviceProvider => new DiscordClient(serviceProvider.GetRequiredService<DiscordConfiguration>()));
            services.TryAddSingleton(_ => new DiscordConfiguration
            {
                TokenType = TokenType.Bot,
                LogLevel = LogLevel.Debug,
                Token = config["Token"],
                AutoReconnect = true,
            });

            return services;
        }

        public static IServiceCollection AddCommandHandler(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<CommandService>();
            services.TryAddSingleton<CommandConfigurationService>();
            services.AddRedis();
            services.TryAddSingleton(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<DiscordClient>();
                return client.UseCommandsNext(new CommandsNextConfiguration
                {
                    Services = serviceProvider,
                    UseDefaultCommandHandler = false,
                });
            });

            return services;
        }

        public static IServiceCollection AddCommand(this IServiceCollection services, Type moduleType)
        {
            _ = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddCommandHandler();
            services.AddSingleton(new CommandModuleRegistration(moduleType));
            return services;
        }

        public static IServiceCollection AddCommand<TModule>(this IServiceCollection services)
        {
            return services.AddCommand(typeof(TModule));
        }
    }
}
