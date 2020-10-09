using System;
using BotV2.BotExtensions;
using BotV2.CommandChecks;
using BotV2.CommandModules;
using BotV2.Models;
using BotV2.Services.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BotV2.Extensions
{
    public static class DiscordServiceExtensions
    {
        public static IServiceCollection AddDiscordClient(this IServiceCollection services, IConfiguration config)
        {
            _ = config ?? throw new ArgumentNullException(nameof(config));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton(serviceProvider => new DiscordClient(serviceProvider.GetRequiredService<DiscordConfiguration>()));
            services.TryAddSingleton(serviceProvider => new DiscordConfiguration
            {
                TokenType = TokenType.Bot,
                LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>(),
                MinimumLogLevel = LogLevel.Debug,
                Token = config["Token"],
                AutoReconnect = true,
            });

            return services;
        }

        public static IServiceCollection TryAddBotExtension<TService>(this IServiceCollection services) where TService : BaseExtension
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddEnumerable(new[] {ServiceDescriptor.Singleton<BaseExtension, TService>()});
            return services;
        }

        public static IServiceCollection AddCommandHandler(this IServiceCollection services)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.TryAddBotExtension<CommandBotExtension>();
            services.AddSingleton<CommandConfigurationService>();
            services.AddRedis();

            // Config
            services.AddSingleton(serviceProvider => new CommandsNextConfiguration
            {
                Services = serviceProvider,
                UseDefaultCommandHandler = false,
                EnableDefaultHelp = false,
            });

            // Commands extension
            services.AddSingleton(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<DiscordClient>();
                var config = serviceProvider.GetRequiredService<CommandsNextConfiguration>();
                return client.UseCommandsNext(config);
            });

            // Default checks
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Singleton<CheckBaseAttribute, HelpRequireMentionAttribute>(),
                ServiceDescriptor.Singleton<CheckBaseAttribute, RequireEnabledAttribute>(),
            });

            // Custom help module
            services.AddCommand<HelpModule>();
            services.AddSingleton<IHelpFormatterFactory, HelpFormatterFactory<DefaultHelpFormatter>>();

            return services;
        }

        public static IServiceCollection AddCommand(this IServiceCollection services, Type moduleType)
        {
            _ = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            _ = services ?? throw new ArgumentNullException(nameof(services));

            services.AddSingleton(new CommandModuleRegistration(moduleType));
            return services;
        }

        public static IServiceCollection AddCommand<TModule>(this IServiceCollection services)
        {
            return services.AddCommand(typeof(TModule));
        }
    }
}