using System;
using System.Threading;
using System.Threading.Tasks;
using BotV2.Models.WarframeInfo;
using Microsoft.Extensions.Options;
using Warframe;

namespace BotV2.Services.WarframeInfo
{
    public class WarframeCetusCycle : IWarframeCycle
    {
        public const string CycleId = "cetus";
        public const string CycleName = "Cetus";

        private readonly IWarframeClient _wfClient;
        private readonly WarframeInfoService _infoService;
        private readonly IOptionsMonitor<WarframeInfoConfig> _config;
        public string Id => WarframeCetusCycle.CycleId;
        public string Name => WarframeCetusCycle.CycleName;

        public WarframeCetusCycle(IWarframeClient wfClient, WarframeInfoService infoService, IOptionsMonitor<WarframeInfoConfig> config)
        {
            this._wfClient = wfClient ?? throw new ArgumentNullException(nameof(wfClient));
            this._infoService = infoService;
            this._config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IWarframeCycleStatus> GetStatus(CancellationToken cancellation = default)
        {
            var status = await this._wfClient.GetCetusStatus(cancellation).ConfigureAwait(false);
            return new WarframeCetusCycleStatus(this._infoService, this._config, status);
        }
    }
}