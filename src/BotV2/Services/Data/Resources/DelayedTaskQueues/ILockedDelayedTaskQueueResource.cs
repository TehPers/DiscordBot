using System.Threading.Tasks;
using BotV2.Models;

namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public interface ILockedDelayedTaskQueueResource<T> : IDelayedTaskQueueResource<T>, IResourceLock
    {
        new Task<Option<T>> TryPopAsync();
    }
}