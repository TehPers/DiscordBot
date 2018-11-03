using System;
using Discord;

namespace Botv2.Interfaces.Logging {
    internal class StandardLogMessage : ILogMessage {
        public string Message { get; }
        public string Source { get; }
        public LogSeverity Severity { get; }
        public DateTimeOffset DateTime { get; }

        public StandardLogMessage(string message, string source, LogSeverity severity) : this(message, source, severity, null) { }
        public StandardLogMessage(string message, string source, LogSeverity severity, Exception exception) {
            this.Message = message;
            this.Source = source;
            this.Severity = severity;
            this.DateTime = DateTimeOffset.UtcNow;
        }
    }
}