using System.Threading.Tasks;

namespace BotV2.Services.Data.Resources.Lists
{
    public interface IListResource<in T> : IVolatileResource
    {
        Task Add(T item);
    }
}