using System;
using System.Text;

namespace TehBot.Core.Logging {
    public class ConsoleLogger : Logger {
        private void Log(string message, string source, string severity, ConsoleColor foreground, Exception exception = null) {
            // Set foreground color
            ConsoleColor origColor = Console.ForegroundColor;
            Console.ForegroundColor = foreground;

            // Build message
            StringBuilder output = new StringBuilder();
            output.AppendLine($"[{severity}] [{source}] [{DateTime.Now:YYYY-MM-DD hh:mm:ss}] - {message}");
            if (exception != null) {
                output.AppendLine(exception.ToString());
            }

            // Output message
            Console.Write(output.ToString());

            // Reset foreground color
            Console.ForegroundColor = origColor;
        }

        public override void Trace(string message, string source) {
            this.Log(message, source, "TRACE", ConsoleColor.Gray);
        }

        public override void Debug(string message, string source) {
            this.Log(message, source, "DEBUG", ConsoleColor.Gray);
        }

        public override void Info(string message, string source) {
            this.Log(message, source, "INFO", ConsoleColor.White);
        }

        public override void Warn(string message, string source) {
            this.Log(message, source, "WARN", ConsoleColor.DarkYellow);
        }

        public override void Error(string message, string source) {
            this.Log(message, source, "ERROR", ConsoleColor.Red);
        }

        public override void Error(string message, string source, Exception exception) {
            this.Error($"{message}\n{exception}", source);
        }

        public override void Critical(string message, string source) {
            this.Log(message, source, "CRITICAL", ConsoleColor.DarkRed);
        }

        public override void Critical(string message, string source, Exception exception) {
            this.Critical($"{message}\n{exception}", source);
        }
    }
}