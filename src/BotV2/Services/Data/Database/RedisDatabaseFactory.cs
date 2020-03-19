using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace BotV2.Services.Data.Database
{
    public class RedisDatabaseFactory : IDatabaseFactory, IAsyncDisposable
    {
        private readonly Lazy<Task<ConnectionMultiplexer>> _multiplexerFactory;

        public RedisDatabaseFactory(IConfiguration configuration)
        {
            this._multiplexerFactory = new Lazy<Task<ConnectionMultiplexer>>(async () =>
            {
                var connectionString = configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("No Redis connection string is supplied in the configuration");
                return await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
            });
        }

        public async Task<IDatabaseAsync> GetDatabase()
        {
            var multiplexer = await this._multiplexerFactory.Value.ConfigureAwait(false);
            return multiplexer.GetDatabase();
        }

        public async ValueTask DisposeAsync()
        {
            if (this._multiplexerFactory.IsValueCreated)
            {
                var multiplexer = await this._multiplexerFactory.Value.ConfigureAwait(false);
                await multiplexer.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
