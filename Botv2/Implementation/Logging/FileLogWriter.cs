using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Botv2.Interfaces.Logging;
using Ninject;

namespace Botv2.Implementation.Logging {
    internal class FileLogWriter : IAsyncLogWriter, IDisposable {
        private readonly TextWriter _fileWriter;
        private readonly Task _consumer;
        private readonly BlockingCollection<ILogMessage> _messageQueue;

        public FileLogWriter([Named("LogPath")] string logPath) {
            string path = Path.Combine(logPath, "log.txt");
            Directory.CreateDirectory(logPath);
            FileStream fileStream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            this._fileWriter = TextWriter.Synchronized(new StreamWriter(fileStream));

            // Message consumer
            this._messageQueue = new BlockingCollection<ILogMessage>();
            this._consumer = this.Consume();
            this._consumer.Start();
        }

        public Task WriteMessage(ILogMessage message) {
            this._messageQueue.Add(message);
            return Task.CompletedTask;
        }

        private async Task Consume() {
            while (!this._messageQueue.IsAddingCompleted || this._messageQueue.Any()) {
                ILogMessage message = this._messageQueue.Take();
                await this._fileWriter.WriteLineAsync($"[{message.Severity}]\t[{message.Source}]\t{message.DateTime:O}\t{message.Message}");

                // Write buffer if there's no more messages queued
                if (!this._messageQueue.Any()) {
                    await this._fileWriter.FlushAsync();
                }
            }
        }

        public void Dispose() {
            this._fileWriter?.Dispose();
            this._consumer?.Dispose();
            this._messageQueue?.Dispose();
        }
    }
}