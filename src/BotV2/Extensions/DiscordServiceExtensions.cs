using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
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
            services.TryAddSingleton(serviceProvider =>
            {
                var client = serviceProvider.GetRequiredService<DiscordClient>();
                var config = serviceProvider.GetService<IConfiguration>();
                var prefixes = new[] { config?["CommandPrefix"] ?? "!" };

                return client.UseCommandsNext(new CommandsNextConfiguration
                {
                    Services = serviceProvider,
                    PrefixResolver = msg => DiscordServiceExtensions.ResolvePrefix(msg, client, prefixes),
                });
            });

            return services;
        }

        private static Task<int> ResolvePrefix(DiscordMessage msg, BaseDiscordClient client, params string[] prefixes)
        {
            _ = prefixes ?? throw new ArgumentNullException(nameof(prefixes));
            _ = client ?? throw new ArgumentNullException(nameof(client));
            _ = msg ?? throw new ArgumentNullException(nameof(msg));

            var trimmed = msg.Content.TrimStart();
            if (trimmed.StartsWith($"{client.CurrentUser.Mention} "))
            {
                return Task.FromResult(client.CurrentUser.Mention.Length + 1);
            }

            foreach (var prefix in prefixes)
            {
                if (!trimmed.StartsWith(prefix))
                {
                    continue;
                }

                if (DiscordServiceExtensions.CommandRegex.Match(trimmed.Substring(prefix.Length)) is { Success: true } match)
                {
                    var cmd = match.Groups["cmd"].Value;
                    if (string.Equals(cmd, "help", StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(-1);
                    }
                }

                return Task.FromResult(prefix.Length);
            }

            return Task.FromResult(-1);
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
