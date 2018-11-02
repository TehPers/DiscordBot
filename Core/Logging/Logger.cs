using System;
using Discord;

namespace TehBot.Core.Logging {
    public abstract class Logger : ILogger {
        public LogSeverity LogLevel { get; set; }

        public void Log(LogMessage message) {
            switch (message.Severity) {
                case LogSeverity.Critical:
                    this.Critical(message.Message, message.Source, message.Exception);
                    break;
                case LogSeverity.Error:
                    this.Error(message.Message, message.Source, message.Exception);
                    break;
                case LogSeverity.Warning:
                    this.Warn(message.Message, message.Source);
                    break;
                case LogSeverity.Info:
                    this.Info(message.Message, message.Source);
                    break;
                case LogSeverity.Verbose:
                    this.Trace(message.Message, message.Source);
                    break;
                case LogSeverity.Debug:
                    this.Debug(message.Message, message.Source);
                    break;
            }
        }

        public abstract void Trace(string message, string source);
        public abstract void Debug(string message, string source);
        public abstract void Info(string message, string source);
        public abstract void Warn(string message, string source);
        public abstract void Error(string message, string source);
        public abstract void Error(string message, string source, Exception exception);
        public abstract void Critical(string message, string source);
        public abstract void Critical(string message, string source, Exception exception);
    }
}