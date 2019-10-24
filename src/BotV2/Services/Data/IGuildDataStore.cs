namespace BotV2.Services.Data
{
    public interface IGuildDataStore : IKeyValueDataStore
    {
        IChannelDataStore GetChannelStore(ulong id);

        IKeyValueDataStore GetUserStore(ulong id);
    }
}