using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BotV2.Services.Data;
using Microsoft.Extensions.Logging;

namespace BotV2.Services.Logging
{
    public class DatabaseLogWriter : IAsyncDisposable
    {
        private readonly IDataService _dataStore;
        private readonly Channel<LogEntry> _messageChannel;
        private readonly CancellationTokenSource _disposeToken;
        private readonly Task _consumer;

        public DatabaseLogWriter(IDataService dataStore)
        {
            this._dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            this._messageChannel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions { SingleReader = true });
            this._disposeToken = new CancellationTokenSource();
            this._consumer = Task.Run(() => this.WriteMessages(this._disposeToken.Token));
        }

        public void AddMessage(LogLevel logLevel, EventId eventId, string category, object? state, string message)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            _ = category ?? throw new ArgumentNullException(nameof(category));

            // Should complete immediately
            var entry = new LogEntry(logLevel, eventId, category, state, message);
            while (!this._messageChannel.Writer.TryWrite(entry)) { }
        }

        private async Task WriteMessages(CancellationToken cancellation)
        {
            var reader = this._messageChannel.Reader;
            while (await reader.WaitToReadAsync(cancellation) && reader.TryRead(out var message))
            {
                var resource = this._dataStore.GetGlobalStore().GetListResource<LogEntry>("logs");
                await resource.Add(message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            this._disposeToken.Dispose();
            this._messageChannel.Writer.Complete();
            await this._consumer;
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class LogEntry
        {
            public LogLevel LogLevel { get; }
            public EventId EventId { get; }
            public string? Category { get; }
            public object State { get; }
            public string Message { get; }

            public LogEntry(LogLevel logLevel, EventId eventId, string? category, object state, string message)
            {
                this.LogLevel = logLevel;
                this.EventId = eventId;
                this.State = state;
                this.Message = message ?? throw new ArgumentNullException(nameof(message));
                this.Category = category ?? throw new ArgumentNullException(nameof(category));
            }
        }
    }
}