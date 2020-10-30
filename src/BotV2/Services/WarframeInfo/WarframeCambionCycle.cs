using System;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models.WarframeInfo;
using Microsoft.Extensions.Options;
using Warframe;

namespace BotV2.Services.WarframeInfo
{
    public class WarframeCambionCycle : IWarframeCycle
    {
        public const string CycleId = "cambion";
        public const string CycleName = "Cambion Drift";

        private readonly IWarframeClient _wfClient;
        private readonly WarframeInfoService _infoService;
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;
        public string Id => WarframeCambionCycle.CycleId;
        public string Name => WarframeCambionCycle.CycleName;

        public WarframeCambionCycle(IWarframeClient wfClient, WarframeInfoService infoService, IOptionsMonitor<WarframeInfoConfig> config)
        {
            this._wfClient = wfClient ?? throw new ArgumentNullException(nameof(wfClient));
            this._infoService = infoService;
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IWarframeCycleStatus> GetStatus(CancellationToken cancellation = default)
        {
            var status = await this._wfClient.GetCambionStatus(cancellation).ConfigureAwait(false);
            return new WarframeCambionCycleStatus(this._infoService, this._config, status);
        }
    }
}