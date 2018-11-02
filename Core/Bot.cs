using System.Threading.Tasks;
using Discord.WebSocket;
using TehBot.Core.Commands;
using TehBot.Core.Commands.Contexts;
using TehBot.Core.Logging;

namespace TehBot.Core {
    public class Bot : IBot {
        private static string Source { get; } = "BOT";

        private readonly ILogger _logger;
        private readonly CommandRegistry _commandRegistry;
        private readonly DiscordSocketClient _client;

        public Bot(ILogger logger, CommandRegistry commandRegistry) {
            this._logger = logger;
            this._commandRegistry = commandRegistry;
            this._client = new DiscordSocketClient();

            this._client.Log += msg => Task.Run(() => this._logger);
            this._client.MessageReceived += this.ClientOnMessageReceived;
        }

        public Task Start() {
            this._logger.Info("Starting bot", Bot.Source);
            //return this._client.StartAsync();
            return Task.CompletedTask;
        }

        public Task Stop() {
            this._logger.Info("Stopping bot", Bot.Source);
            //return this._client.StopAsync();
            return Task.CompletedTask;
        }

        private async Task ClientOnMessageReceived(SocketMessage socketMessage) {
            bool executed = await this._commandRegistry.Execute(socketMessage.Content, new MessageCommandContext(socketMessage));

            if (!executed) {
                this._logger.Info($"Skipped message {socketMessage.Content}", Bot.Source);
            }
        }
    }
}