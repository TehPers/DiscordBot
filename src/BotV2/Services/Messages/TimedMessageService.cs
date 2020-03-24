using System;
using System.Threading.Tasks;
using BotV2.Models;
using BotV2.Services.Data;
using BotV2.Services.Data.Resources.DelayedTaskQueues;
using DSharpPlus.Entities;

namespace BotV2.Services.Messages
{
    public class TimedMessageService
    {
        internal const string BaseKey = "timed_messages";
        internal const string RemoveQueueKey = TimedMessageService.BaseKey + ":remove";

        private readonly IDataService _dataService;

        public TimedMessageService(IDataService dataService)
        {
            this._dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        private IDelayedTaskQueueResource<MessagePointer> GetRemoveQueue()
        {
            var globalStore = this._dataService.GetGlobalStore();
            return globalStore.GetDelayedTaskQueueResource<MessagePointer>($"{TimedMessageService.RemoveQueueKey}");
        }

        public async Task RemoveAfter(DiscordMessage message, DateTimeOffset removeTime)
        {
            var messagePointer = new MessagePointer(message);
            var removeQueue = this.GetRemoveQueue();
            await removeQueue.AddAsync(messagePointer, removeTime).ConfigureAwait(false);
        }
    }
}