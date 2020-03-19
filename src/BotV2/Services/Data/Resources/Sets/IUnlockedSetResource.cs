namespace BotV2.Services.Data.Resources.Sets
{
    public interface IUnlockedSetResource<T> : ISetResource<T>, ILockableResource<ILockedSetResource<T>>
    {
    }
}