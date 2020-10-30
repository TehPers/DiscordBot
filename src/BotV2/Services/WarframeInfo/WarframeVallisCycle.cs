using System;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models.WarframeInfo;
using Microsoft.Extensions.Options;
using Warframe;

namespace BotV2.Services.WarframeInfo
{
    public class WarframeVallisCycle : IWarframeCycle
    {
        public const string CycleId = "vallis";
        public const string CycleName = "Orb Vallis";

        private readonly IWarframeClient _wfClient;
        private readonly WarframeInfoService _infoService;
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;
        public string Id => WarframeVallisCycle.CycleId;
        public string Name => WarframeVallisCycle.CycleName;

        public WarframeVallisCycle(IWarframeClient wfClient, WarframeInfoService infoService, IOptionsMonitor<WarframeInfoConfig> config)
        {
            this._wfClient = wfClient ?? throw new ArgumentNullException(nameof(wfClient));
            this._infoService = infoService;
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IWarframeCycleStatus> GetStatus(CancellationToken cancellation = default)
        {
            var status = await this._wfClient.GetVallisStatus(cancellation).ConfigureAwait(false);
            return new WarframeVallisCycleStatus(this._infoService, this._config, status);
        }
    }
}