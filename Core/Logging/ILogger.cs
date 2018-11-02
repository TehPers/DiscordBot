using System;
using Discord;

namespace TehBot.Core.Logging {
    public interface ILogger {
        LogSeverity LogLevel { get; set; }

        void Log(LogMessage message);

        void Trace(string message, string source);

        void Debug(string message, string source);

        void Info(string message, string source);

        void Warn(string message, string source);

        void Error(string message, string source);
        void Error(string message, string source, Exception exception);

        void Critical(string message, string source);
        void Critical(string message, string source, Exception exception);
    }
}