using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Caching.Memory;
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

        private readonly IMemoryCache _cache;
        private readonly CancellationTokenSource _disposeSource;

        public event EventHandler<HttpRequestEventArgs> MakingHttpRequest;

        public WarframeClient(WarframePlatform platform, Func<Uri, CancellationToken, Task<HttpResponseMessage>> request)
            : this(WarframeClient.GetEndpointFromPlatform(platform), request)
        {
        }

        public WarframeClient(Uri baseEndpoint, Func<Uri, CancellationToken, Task<HttpResponseMessage>> request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            _ = baseEndpoint ?? throw new ArgumentNullException(nameof(baseEndpoint));

            this._cache = new MemoryCache(new MemoryCacheOptions());
            this._disposeSource = new CancellationTokenSource();

            // Retry on failed download
            var rng = new Random();
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<OperationCanceledException>()
                .OrResult(result => !result.IsSuccessStatusCode)
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 500 + rng.Next(100)));

            // Timeout after a certain amount of time
            var timeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

            var requestPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);
            var cacheProvider = new MemoryCacheProvider(this._cache);
            var serializer = new JsonSerializer();

            this._alertsProvider = new ApiProvider<List<Alert>>(request, requestPolicy, cacheProvider, TimeSpan.FromMinutes(5), serializer, new Uri(baseEndpoint, "alerts"));
            this._invasionsProvider = new ApiProvider<List<Invasion>>(request, requestPolicy, cacheProvider, TimeSpan.FromMinutes(5), serializer, new Uri(baseEndpoint, "invasions"));
            this._cetusStatusProvider = new ApiProvider<CetusCycle>(request, requestPolicy, cacheProvider, TimeSpan.FromSeconds(15), serializer, new Uri(baseEndpoint, "cetusCycle"));

            this._alertsProvider.MakingHttpRequest += (_, e) => this.OnMakingHttpRequest(e);
            this._invasionsProvider.MakingHttpRequest += (_, e) => this.OnMakingHttpRequest(e);
            this._cetusStatusProvider.MakingHttpRequest += (_, e) => this.OnMakingHttpRequest(e);
        }

        public async Task<IEnumerable<Alert>> GetAlertsAsync(CancellationToken cancellation = default)
        {
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(this._disposeSource.Token, cancellation))
            {
                return await this._alertsProvider.GetResult(linkedSource.Token).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<Invasion>> GetInvasionsAsync(CancellationToken cancellation = default)
        {
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(this._disposeSource.Token, cancellation))
            {
                return await this._invasionsProvider.GetResult(linkedSource.Token).ConfigureAwait(false);
            }
        }

        public async Task<CetusCycle> GetCetusStatus(CancellationToken cancellation = default)
        {
            using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(this._disposeSource.Token, cancellation))
            {
                return await this._cetusStatusProvider.GetResult(linkedSource.Token).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            this._disposeSource.Cancel();

            this._alertsProvider.Dispose();
            this._invasionsProvider.Dispose();
            this._cetusStatusProvider.Dispose();

            this._cache.Dispose();
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