using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace BotV2.Extensions
{
    public static class InteractivityExtensions
    {
        public static async Task<DiscordMessage> WaitForMessageAsync(this DiscordClient client, Func<DiscordMessage, bool> predicate, CancellationToken cancellation = default)
        {
            _ = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _ = client ?? throw new ArgumentNullException(nameof(client));

            using var waiter = new MessageWaiter(client, predicate, cancellation);
            return await waiter.Task.ConfigureAwait(false);
        }

        private sealed class MessageWaiter : IDisposable
        {
            private readonly Func<DiscordMessage, bool> _predicate;
            private readonly TaskCompletionSource<DiscordMessage> _completionSource;
            private readonly SemaphoreSlim _messageCreatedSemaphore;
            private readonly CancellationTokenSource _disposeSource;
            private readonly CancellationTokenSource _linkedSource;
            private bool _disposed;

            public Task<DiscordMessage> Task => this._completionSource.Task;

            public MessageWaiter(DiscordClient client, Func<DiscordMessage, bool> predicate, CancellationToken cancellation = default)
            {
                this._predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
                this._completionSource = new TaskCompletionSource<DiscordMessage>();
                this._disposeSource = new CancellationTokenSource();
                this._linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation, this._disposeSource.Token);
                this._messageCreatedSemaphore = new SemaphoreSlim(1, 1);
                this._disposed = false;

                cancellation.ThrowIfCancellationRequested();
                client.MessageCreated += this.ClientOnMessageCreated;
            }

            private async Task ClientOnMessageCreated(MessageCreateEventArgs args)
            {
                if (this.Task.IsCompleted || this.Task.IsCanceled || this.Task.IsFaulted)
                {
                    return;
                }

                await this._messageCreatedSemaphore.WaitAsync(this._linkedSource.Token).ConfigureAwait(false);
                try
                {
                    if (this.Task.IsCompleted || this.Task.IsCanceled || this.Task.IsFaulted)
                    {
                        return;
                    }

                    if (this._predicate(args.Message))
                    {
                        this._completionSource.SetResult(args.Message);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    if (!this._disposed)
                    {
                        this._messageCreatedSemaphore.Release();
                    }
                }
            }

            public void Dispose()
            {
                this._disposed = true;
                this._disposeSource.Dispose();
                this._linkedSource.Dispose();
                this._messageCreatedSemaphore.Dispose();
            }
        }
    }
}