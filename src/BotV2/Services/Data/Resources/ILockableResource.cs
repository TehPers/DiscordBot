using System;
using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources
{
    public interface ILockableResource<T>
    {
        Task<T> Reserve(TimeSpan expiry);
    }
}