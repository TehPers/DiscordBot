namespace BotV2.Services.Data.Resources.DelayedTaskQueues
{
    public interface IUnlockedDelayedTaskQueueResource<T> : IDelayedTaskQueueResource<T>, ILockableResource<ILockedDelayedTaskQueueResource<T>>
    {
    }
}