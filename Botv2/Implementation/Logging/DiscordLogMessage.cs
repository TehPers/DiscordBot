using System;
using System.Linq;
using Discord;

namespace Botv2.Interfaces.Logging {
    internal class DiscordLogMessage : ILogMessage {
        public string Message { get; }
        public string Source { get; }
        public LogSeverity Severity { get; }
        public DateTimeOffset DateTime { get; }

        public DiscordLogMessage(LogMessage message) {
            this.Message = string.Join('\n', new[] { message.Message, message.Exception?.ToString() }.Where(s => !string.IsNullOrEmpty(s)));
            this.Source = message.Source;
            this.Severity = message.Severity;
            this.DateTime = DateTimeOffset.UtcNow;
        }
    }
}