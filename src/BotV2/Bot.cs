using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace BotV2
{
    internal sealed class Bot
    {
        private readonly DiscordClient _client;
        private readonly ILogger<Bot> _logger;
        private int _running;

        public bool IsRunning => this._running > 0;

        public Bot(DiscordClient client, IEnumerable<BaseExtension> extensions, ILogger<Bot> logger)
        {
            this._client = client ?? throw new ArgumentNullException(nameof(client));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this._running = 0;

            // Logging
            client.ClientErrored += (sender, args) => this.LogEventAsync("Client", LogLevel.Error, $"An error occurred during {args.EventName}", args.Exception);
            client.SocketErrored += (sender, args) => this.LogEventAsync("Socket", LogLevel.Error, "Socket connection errored", args.Exception);
            client.GuildUnavailable += (sender, args) => this.LogEventAsync("Discord", LogLevel.Warning, $"Guild became {(args.Unavailable ? "unavailable" : "available")}: {args.Guild.Name} ({args.Guild.Id})");
            client.UnknownEvent += (sender, args) => this.LogEventAsync("Unknown", LogLevel.Information, $"An unknown event occurred: [{args.EventName}] {args.Json}");

            // Extensions
            foreach (var extension in extensions)
            {
                client.AddExtension(extension);
            }
        }

        public async Task Start()
        {
            if (Interlocked.Exchange(ref this._running, 1) == 0)
            {
                try
                {
                    this._logger.LogInformation("Starting");
                    await this._client.ConnectAsync().ConfigureAwait(false);
                    this._logger.LogInformation("Started");
                }
                catch
                {
                    this._running = 0;
                    throw;
                }
            }
        }

        private Task LogEventAsync(string eventName, LogLevel level, string message, Exception? exception = null)
        {
            this.LogEvent(eventName, level, message, exception);
            return Task.CompletedTask;
        }

        private void LogEvent(string eventName, LogLevel level, string message, Exception? exception = null)
        {
            this._logger.Log(level, exception, $"[{eventName}] {message}");
        }
    }
}