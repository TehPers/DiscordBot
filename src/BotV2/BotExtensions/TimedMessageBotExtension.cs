using System;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Extensions;
using BotV2.Models;
using BotV2.Services.Data;
using BotV2.Services.Data.Resources.DelayedTaskQueues;
using BotV2.Services.Messages;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace BotV2.BotExtensions
{
    public class TimedMessageBotExtension : MultiThreadedBotExtension
    {
        private readonly IDataService _dataService;
        private readonly ILogger<TimedMessageBotExtension> _logger;

        public TimedMessageBotExtension(IDataService dataService, ILogger<TimedMessageBotExtension> logger)
        {
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override void Setup(DiscordClient client)
        {
            base.Setup(client);

            client.Ready += (sender, args) =>
            {
                this.RunParallel(this.RemoveExpired);
                return Task.CompletedTask;
            };
        }

        private async Task RemoveExpired(CancellationToken cancellation = default)
        {
            var removeQueue = this.GetRemoveQueue();

            while (true)
            {
                cancellation.ThrowIfCancellationRequested();

                await foreach (var removed in removeQueue.PopAvailable(cancellation))
                {
                    try
                    {
                        if (!(await removed.TryGetMessage(this.Client).ConfigureAwait(false) is { } message))
                        {
                            continue;
                        }

                        await message.TryDeleteAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogWarning(ex, "Unable to delete expired message");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellation).ConfigureAwait(false);
            }
        }

        private IDelayedTaskQueueResource<MessagePointer> GetRemoveQueue()
        {
            var globalStore = this._dataService.GetGlobalStore();
            return globalStore.GetDelayedTaskQueueResource<MessagePointer>($"{TimedMessageService.RemoveQueueKey}");
        }
    }
}