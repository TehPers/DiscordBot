namespace BotV2.Services.Data.Resources.SortedSets
{
    public interface IUnlockedSortedSetResource<T> : ISortedSetResource<T>, ILockableResource<ILockedSortedSetResource<T>>
        where T : IScored
    {
    }
}