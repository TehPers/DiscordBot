using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BotV2.Services.Data;
using Microsoft.Extensions.Logging;

namespace BotV2.Services.Logging
{
    public class DatabaseLoggerProvider : ILoggerProvider, IAsyncDisposable
    {
        private readonly ConcurrentDictionary<string, DatabaseLogger> _loggers;
        private readonly DatabaseLogWriter _logWriter;

        public DatabaseLoggerProvider(IDataService dataService)
        {
            _ = dataService ?? throw new ArgumentNullException(nameof(dataService));

            this._loggers = new ConcurrentDictionary<string, DatabaseLogger>();
            this._logWriter = new DatabaseLogWriter(dataService);
        }

        public ILogger CreateLogger(string category)
        {
            _ = category ?? throw new ArgumentNullException(nameof(category));
            return this._loggers.GetOrAdd(category, _ => new DatabaseLogger(this._logWriter, category));
        }
        
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return this._logWriter.DisposeAsync();
        }
    }
}