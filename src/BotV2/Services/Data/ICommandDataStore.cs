namespace BotV2.Services.Data
{
    public interface ICommandDataStore : IKeyValueDataStore
    {
        IGuildDataStore GetGuildStore(ulong id);

        IKeyValueDataStore GetUserStore(ulong id);
    }
}