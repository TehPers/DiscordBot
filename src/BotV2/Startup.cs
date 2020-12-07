using System;
using System.IO;
using System.Threading.Tasks;
using BotV2.CommandModules;
using BotV2.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BotV2
{
    internal static class Startup
    {
#if DEBUG
        private const bool Release = false;
#else
        private const bool Release = true;
#endif

        private static async Task Main()
        {
            Console.WriteLine("Bot V2");

            await using (Startup.RegisterServices().BuildServiceProvider().ConfigureAwait(false, out var services))
            {
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(typeof(Startup));
                logger.LogTrace("Built service provider");
                logger.LogInformation("Starting...");

                var settings = services.GetRequiredService<JsonSerializerSettings>();
                JsonConvert.DefaultSettings = () => settings;

                try
                {
                    var bot = services.GetRequiredService<Bot>();
                    await bot.Start().ConfigureAwait(false);

                    // Wait until bot is finished
                    while (bot.IsRunning)
                    {
                    }
                }
                catch (Exception ex) when (Startup.LogError(logger, ex, "Exception thrown during bot initialization"))
                {
                }
            }

            // Wait until closed
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static bool LogError(ILogger logger, Exception ex, string? message = null)
        {
            logger.LogError(ex, message);
            return true;
        }

        private static IServiceCollection RegisterServices()
        {
            var services = new ServiceCollection();
            var configuration = Startup.BuildConfiguration();

            // Logging and configuration
            services.AddSingleton(configuration);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });

            // Bot
            services.AddSingleton<Bot>();
            services.AddDiscordClient(configuration.GetSection("Discord"));
            services.AddCommandHandler();

            // Data
            services.AddRedis();

            // Commands
            services.AddFireEmblem(configuration);
            services.AddCommand<AdminModule>();
            services.AddWarframeInfo(configuration);

            return services;
        }

        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "Configs"))
                .AddJsonFile("appsettings.json", true, !Startup.Release);

            // Environment-specific config files
#pragma warning disable 162
            // ReSharper disable HeuristicUnreachableCode
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Startup.Release)
            {
                builder.AddJsonFile("appsettings.Release.json", false, false);
            }
            else
            {
                builder.AddJsonFile("appsettings.Debug.json", false, true);
            }
            // ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162

            return builder.Build();
        }
    }
}