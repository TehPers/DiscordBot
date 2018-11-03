using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Botv2.Interfaces.Logging;
using Discord;
using Ninject;

namespace Botv2.Implementation.Logging {
    internal class ConsoleLogWriter : IAsyncLogWriter {
        private readonly LogSeverity _severity;
        private readonly object _writeLock = new object();

        public ConsoleLogWriter([Named("Severity")] LogSeverity severity) {
            this._severity = severity;
        }

        public Task WriteMessage(ILogMessage message) {
            if (message.Severity <= this._severity) {
                lock (this._writeLock) {
                    // TODO: proper indentation
                    Console.ForegroundColor = ConsoleLogWriter.GetColor(message.Severity);
                    Console.WriteLine($"[{message.Severity}]\t[{message.Source}]\t{message.DateTime:yyyy-MM-dd HH:mm:ss}\t{message.Message}");
                }
            }

            return Task.CompletedTask;
        }

        private static ConsoleColor GetColor(LogSeverity severity) {
            switch (severity) {
                case LogSeverity.Critical:
                    return ConsoleColor.DarkRed;
                case LogSeverity.Error:
                    return ConsoleColor.Red;
                case LogSeverity.Warning:
                    return ConsoleColor.DarkYellow;
                case LogSeverity.Info:
                    return ConsoleColor.White;
                case LogSeverity.Verbose:
                    return ConsoleColor.Gray;
                case LogSeverity.Debug:
                    return ConsoleColor.DarkGray;
                default:
                    // Don't just throw because the severity is unknown
                    return ConsoleColor.DarkGreen;
            }
        }
    }
}
