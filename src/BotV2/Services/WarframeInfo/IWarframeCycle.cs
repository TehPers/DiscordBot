using System.Threading;
using System.Threading.Tasks;

namespace BotV2.Services.WarframeInfo
{
    public interface IWarframeCycle
    {
        string Id { get; }

        string Name { get; }

        Task<IWarframeCycleStatus> GetStatus(CancellationToken cancellation = default);
    }
}