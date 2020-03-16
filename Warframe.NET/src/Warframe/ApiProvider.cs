using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Bulkhead;
using Polly.Caching.Memory;

namespace Warframe
{
    internal sealed class ApiProvider<TModel> : IDisposable
    {
        private readonly Func<Uri, CancellationToken, Task<HttpResponseMessage>> _request;
        private readonly JsonSerializer _serializer;
        private readonly Uri _requestUri;
        private readonly IMemoryCache _cache;
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;
        private readonly CancellationTokenSource _disposeTokenSource;

        public event EventHandler<HttpRequestEventArgs> MakingHttpRequest;

        public ApiProvider(Func<Uri, CancellationToken, Task<HttpResponseMessage>> request, JsonSerializer serializer, Uri requestUri)
        {
            this._request = request ?? throw new ArgumentNullException(nameof(request));
            this._serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this._requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));

            // Retry on failed download
            var rng = new Random();
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<OperationCanceledException>()
                .Or<BulkheadRejectedException>()
                .OrResult(result => !result.IsSuccessStatusCode)
                .WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 500 + rng.Next(100)));

            // Cache result
            this._cache = new MemoryCache(new MemoryCacheOptions());
            var cacheProvider = new MemoryCacheProvider(this._cache);
            var cachePolicy = Policy
                .CacheAsync<HttpResponseMessage>(cacheProvider, TimeSpan.FromMinutes(5));

            // Limit to one HTTP request at a time, with a queue of up to 3 requests
            var bulkheadPolicy = Policy
                .BulkheadAsync<HttpResponseMessage>(1, 3);

            // Timeout after a certain amount of time
            var timeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

            this._policy = Policy.WrapAsync(retryPolicy, cachePolicy, bulkheadPolicy, timeoutPolicy);
            this._disposeTokenSource = new CancellationTokenSource();
        }

        public async Task<TModel> GetResult(CancellationToken cancellation)
        {
            using (var source = CancellationTokenSource.CreateLinkedTokenSource(this._disposeTokenSource.Token, cancellation))
            using (var response = await this._policy.ExecuteAsync(this.ExecutePolicy, source.Token).ConfigureAwait(false))
            using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var contentReader = new StreamReader(contentStream))
            using (var jsonReader = new JsonTextReader(contentReader))
            {
                return this._serializer.Deserialize<TModel>(jsonReader);
            }
        }

        private Task<HttpResponseMessage> ExecutePolicy(CancellationToken c)
        {
            this.OnMakingRequest(new HttpRequestEventArgs(this._requestUri, HttpMethod.Get));
            return this._request(this._requestUri, c);
        }

        public void Dispose()
        {
            this._disposeTokenSource.Dispose();
            this._cache.Dispose();
        }

        private void OnMakingRequest(HttpRequestEventArgs e)
        {
            this.MakingHttpRequest?.Invoke(this, e);
        }
    }
}
