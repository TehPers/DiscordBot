using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Botv2.Interfaces.Logging {
    public interface ILogMessage {
        /// <summary>The message to display.</summary>
        string Message { get; }

        /// <summary>The message source.</summary>
        string Source { get; }

        /// <summary>The severity of the message.</summary>
        LogSeverity Severity { get; }

        /// <summary>The date and time the message was logged.</summary>
        DateTimeOffset DateTime { get; }
    }
}
