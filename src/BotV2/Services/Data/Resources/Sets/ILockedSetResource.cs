namespace BotV2.Services.Data.Resources.Sets
{
    public interface ILockedSetResource<T> : ISetResource<T>, IResourceLock
    {
    }
}