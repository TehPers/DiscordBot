using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BotV2.Services.Logging
{
    public class DatabaseLogger : ILogger
    {
        private readonly DatabaseLogWriter _logWriter;
        private readonly string _category;
        private readonly ConcurrentStack<object?> _states;

        public DatabaseLogger(DatabaseLogWriter logWriter, string category)
        {
            this._logWriter = logWriter;
            this._category = category;
            this._states = new ConcurrentStack<object?>();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            if (!this.IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            var states = this._states.Reverse().ToList();
            states.Add(state);
            this._logWriter.AddMessage(logLevel, eventId, this._category, states, message);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None
                && logLevel >= LogLevel.Warning;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            this._states.Push(state);
            return new Scope(this._states);
        }

        private class Scope : IDisposable
        {
            private readonly ConcurrentStack<object?> _stack;

            public Scope(ConcurrentStack<object?> stack)
            {
                this._stack = stack ?? throw new ArgumentNullException(nameof(stack));
            }

            public void Dispose()
            {
                this._stack.TryPop(out _);
            }
        }
    }
}
