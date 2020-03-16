using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Warframe.World.Models;

namespace Warframe
{
    public sealed class WarframeClient : IWarframeClient
    {
        private static Uri WarframeStatusEndpoint { get; } = new Uri("https://api.warframestat.us/");
        public static Uri PcEndpoint { get; } = new Uri(WarframeClient.WarframeStatusEndpoint, "pc/");
        public static Uri Ps4Endpoint { get; } = new Uri(WarframeClient.WarframeStatusEndpoint, "ps4/");
        public static Uri Xbox1Endpoint { get; } = new Uri(WarframeClient.WarframeStatusEndpoint, "xb1/");
        public static Uri SwitchEndpoint { get; } = new Uri(WarframeClient.WarframeStatusEndpoint, "swi/");

        private readonly ApiProvider<List<Alert>> _alertsProvider;
        private readonly ApiProvider<List<Invasion>> _invasionsProvider;
        private readonly ApiProvider<CetusCycle> _cetusStatusProvider;

        public event EventHandler<HttpRequestEventArgs> MakingHttpRequest;

        public WarframeClient(WarframePlatform platform, Func<Uri, CancellationToken, Task<HttpResponseMessage>> request)
            : this(WarframeClient.GetEndpointFromPlatform(platform), request)
        {
        }

        public WarframeClient(Uri baseEndpoint, Func<Uri, CancellationToken, Task<HttpResponseMessage>> request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            _ = baseEndpoint ?? throw new ArgumentNullException(nameof(baseEndpoint));

            var serializer = new JsonSerializer();

            this._alertsProvider = new ApiProvider<List<Alert>>(request, serializer, new Uri(baseEndpoint, "alerts"));
            this._invasionsProvider = new ApiProvider<List<Invasion>>(request, serializer, new Uri(baseEndpoint, "invasions"));
            this._cetusStatusProvider = new ApiProvider<CetusCycle>(request, serializer, new Uri(baseEndpoint, "cetusCycle"));

            this._alertsProvider.MakingHttpRequest += (_, e) => this.OnMakingHttpRequest(e);
            this._invasionsProvider.MakingHttpRequest += (_, e) => this.OnMakingHttpRequest(e);
            this._cetusStatusProvider.MakingHttpRequest += (_, e) => this.OnMakingHttpRequest(e);
        }

        public async Task<IEnumerable<Alert>> GetAlertsAsync(CancellationToken cancellation = default)
        {
            return await this._alertsProvider.GetResult(cancellation).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Invasion>> GetInvasionsAsync(CancellationToken cancellation = default)
        {
            return await this._invasionsProvider.GetResult(cancellation).ConfigureAwait(false);
        }

        public async Task<CetusCycle> GetCetusStatus(CancellationToken cancellation = default)
        {
            return await this._cetusStatusProvider.GetResult(cancellation).ConfigureAwait(false);
        }

        public void Dispose()
        {
            this._alertsProvider.Dispose();
            this._invasionsProvider.Dispose();
            this._cetusStatusProvider.Dispose();
        }

        private void OnMakingHttpRequest(HttpRequestEventArgs e)
        {
            this.MakingHttpRequest?.Invoke(this, e);
        }

        private static Uri GetEndpointFromPlatform(WarframePlatform platform)
        {
            switch (platform)
            {
                case WarframePlatform.Pc:
                    return WarframeClient.PcEndpoint;
                case WarframePlatform.Ps4:
                    return WarframeClient.Ps4Endpoint;
                case WarframePlatform.Xbox1:
                    return WarframeClient.Xbox1Endpoint;
                case WarframePlatform.Switch:
                    return WarframeClient.SwitchEndpoint;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }
    }
}