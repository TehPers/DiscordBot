using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Botv2.Interfaces.Logging {
    internal class AsyncLogger : IAsyncLogger {
        private readonly IAsyncLogWriter[] _writers;

        public AsyncLogger(IAsyncLogWriter[] writers) {
            this._writers = writers;
        }

        public Task Log(string message, string source, LogSeverity severity) {
            return this.Log(new StandardLogMessage(message, source, severity));
        }

        public Task Log(string message, string source, LogSeverity severity, Exception exception) {
            return this.Log(new StandardLogMessage(message, source, severity, exception));
        }

        public Task Log(ILogMessage message) {
            return Task.WhenAll(this._writers.Select(writer => writer.WriteMessage(message)));
        }
    }
}