using System;
using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources
{
    public interface IResourceLock : IAsyncDisposable
    {
        Task<bool> ExtendLock(TimeSpan addedTime);
    }
}