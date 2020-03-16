using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;

namespace BotV2.BotExtensions
{
    public abstract class MultiThreadedBotExtension : BaseExtension, IAsyncDisposable
    {
        private readonly CancellationTokenSource _tokenSource;
        private readonly List<Task> _parallelTasks;

        protected MultiThreadedBotExtension()
        {
            this._tokenSource = new CancellationTokenSource();
            this._parallelTasks = new List<Task>();
        }

        protected override void Setup(DiscordClient client)
        {
            if (this.Client != null)
            {
                throw new InvalidOperationException("Extension has already been setup");
            }

            this.Client = client;
        }

        protected void RunParallel(Func<CancellationToken, Task> taskFactory)
        {
            this._parallelTasks.Add(Task.Run(() => taskFactory(this._tokenSource.Token), this._tokenSource.Token));
        }

        public virtual async ValueTask DisposeAsync()
        {
            try
            {
                this._tokenSource.Cancel();
                await Task.WhenAll(this._parallelTasks);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}