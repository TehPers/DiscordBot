using System;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Botv2.Interfaces.Logging {
    public interface IAsyncLogger {
        /// <summary>Logs a message, writing it to all logs.</summary>
        /// <param name="message">The message to write to the logs.</param>
        /// <param name="source">The message source.</param>
        /// <param name="severity">The severity of the message.</param>
        Task Log(string message, string source, LogSeverity severity);

        /// <summary>Logs a message, writing it to all logs.</summary>
        /// <param name="message">The message to write to the logs.</param>
        /// <param name="source">The message source.</param>
        /// <param name="severity">The severity of the message.</param>
        /// <param name="exception">The exception to write.</param>
        Task Log(string message, string source, LogSeverity severity, Exception exception);

        /// <summary>Logs a message, writing it to all logs.</summary>
        /// <param name="message">The message to write to the logs..</param>
        Task Log(ILogMessage message);
    }
}
