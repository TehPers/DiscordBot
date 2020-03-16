using BotV2.Services.Data.Resources;
using BotV2.Services.Data.Resources.DelayedTaskQueues;
using BotV2.Services.Data.Resources.HashTables;
using BotV2.Services.Data.Resources.Lists;
using BotV2.Services.Data.Resources.Objects;
using BotV2.Services.Data.Resources.Sets;
using BotV2.Services.Data.Resources.SortedSets;

namespace BotV2.Services.Data
{
    public interface IKeyValueDataStore
    {
        IObjectResource<T> GetObjectResource<T>(string key);

        IUnlockedDelayedTaskQueueResource<T> GetDelayedTaskQueueResource<T>(string key);

        IListResource<T> GetListResource<T>(string key);

        IUnlockedSetResource<T> GetSetResource<T>(string key);

        IUnlockedSortedSetResource<T> GetSortedSetResource<T>(string key) where T : IScored;

        IHashTableResource<T> GetTableResource<T>(string key);
    }
}