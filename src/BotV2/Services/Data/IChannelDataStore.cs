namespace BotV2.Services.Data
{
    public interface IChannelDataStore : IKeyValueDataStore
    {
        IKeyValueDataStore GetUserStore(ulong id);
    }
}