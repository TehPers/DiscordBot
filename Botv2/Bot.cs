using System.Threading.Tasks;
using Botv2.Interfaces.Client;
using Botv2.Interfaces.Logging;
using Discord;
using Discord.WebSocket;

namespace Botv2 {
    internal class Bot {
        private readonly ITokenProvider _tokenProvider;
        private readonly DiscordSocketClient _client;

        public Bot(IAsyncLogger logger, ITokenProvider tokenProvider) {
            this._tokenProvider = tokenProvider;
            this._client = new DiscordSocketClient();
            this._client.Log += message => logger.Log(new DiscordLogMessage(message));
        }

        public async Task Run() {
            // Log into Discord
            await this._client.LoginAsync(TokenType.Bot, this._tokenProvider.GetToken());
            await this._client.StartAsync();
        }
    }
}