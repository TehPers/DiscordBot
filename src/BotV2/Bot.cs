using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
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
            client.ClientErrored += args => this.LogEventAsync("Client", LogLevel.Error, $"An error occurred during {args.EventName}", args.Exception);
            client.SocketErrored += args => this.LogEventAsync("Socket", LogLevel.Error, "Socket connection errored", args.Exception);
            client.GuildUnavailable += args => this.LogEventAsync("Discord", LogLevel.Warning, $"Guild became {(args.Unavailable ? "unavailable" : "available")}: {args.Guild.Name} ({args.Guild.Id})");
            client.UnknownEvent += args => this.LogEventAsync("Unknown", LogLevel.Information, $"An unknown event occurred: [{args.EventName}] {args.Json}");
            client.DebugLogger.LogMessageReceived += this.DebugLoggerOnLogMessageReceived;

            // Extensions
            foreach (var extension in extensions)
            {
                client.AddExtension(extension);
            }
        }

        private void DebugLoggerOnLogMessageReceived(object? sender, DebugLogMessageEventArgs e)
        {
            var level = e.Level switch
            {
                DSharpPlus.LogLevel.Debug => LogLevel.Trace,
                DSharpPlus.LogLevel.Info => LogLevel.Information,
                DSharpPlus.LogLevel.Warning => LogLevel.Warning,
                DSharpPlus.LogLevel.Error => LogLevel.Error,
                DSharpPlus.LogLevel.Critical => LogLevel.Critical,
                _ => throw new ArgumentOutOfRangeException()
            };

            this.LogEvent(e.Application, level, $"[{e.Timestamp:O}] {e.Message}", e.Exception);
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
