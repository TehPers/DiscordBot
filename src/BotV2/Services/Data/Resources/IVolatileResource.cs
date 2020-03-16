using System;
using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources
{
    public interface IVolatileResource
    {
        Task<bool> SetExpiry(DateTimeOffset expiry);
    }
}