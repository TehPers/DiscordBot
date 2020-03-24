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
using Polly.Caching;
using Polly.Caching.Memory;

namespace Warframe
{
    internal sealed class ApiProvider<TModel> : IDisposable
    {
        private readonly Func<Uri, CancellationToken, Task<HttpResponseMessage>> _request;
        private readonly IAsyncPolicy<TModel> _cachePolicy;
        private readonly IAsyncPolicy<HttpResponseMessage> _requestPolicy;
        private readonly JsonSerializer _serializer;
        private readonly Uri _requestUri;
        private readonly CancellationTokenSource _disposeTokenSource;

        public event EventHandler<HttpRequestEventArgs> MakingHttpRequest;

        public ApiProvider(Func<Uri, CancellationToken, Task<HttpResponseMessage>> request, IAsyncPolicy<HttpResponseMessage> requestPolicy, IAsyncCacheProvider cacheProvider, TimeSpan cacheTtl, JsonSerializer serializer, Uri requestUri)
        {
            _ = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            this._request = request ?? throw new ArgumentNullException(nameof(request));
            this._requestPolicy = requestPolicy ?? throw new ArgumentNullException(nameof(requestPolicy));
            this._serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this._requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
            this._disposeTokenSource = new CancellationTokenSource();

            // Cache result
            var cachePolicy = Policy
                .CacheAsync<TModel>(cacheProvider, cacheTtl, context => context.OperationKey);

            // Limit to one HTTP request at a time, with a queue of up to 3 requests
            var bulkheadPolicy = Policy
                .BulkheadAsync<TModel>(1, 10);

            this._cachePolicy = Policy.WrapAsync(bulkheadPolicy, cachePolicy);
        }

        public async Task<TModel> GetResult(CancellationToken cancellation)
        {
            using (var source = CancellationTokenSource.CreateLinkedTokenSource(this._disposeTokenSource.Token, cancellation))
            {
                return await this._cachePolicy.ExecuteAsync(this.Request, new Context(this._requestUri.AbsoluteUri), source.Token).ConfigureAwait(false);
            }
        }

        private async Task<TModel> Request(Context context, CancellationToken cancellation)
        {
            using (var response = await this._requestPolicy.ExecuteAsync((_, c) => this.ExecuteRequest(c), new Context(context.OperationKey), cancellation).ConfigureAwait(false))
            using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var contentReader = new StreamReader(contentStream))
            using (var jsonReader = new JsonTextReader(contentReader))
            {
                return this._serializer.Deserialize<TModel>(jsonReader);
            }
        }

        private Task<HttpResponseMessage> ExecuteRequest(CancellationToken cancellation)
        {
            this.OnMakingRequest(new HttpRequestEventArgs(this._requestUri, HttpMethod.Get));
            return this._request(this._requestUri, cancellation);
        }

        public void Dispose()
        {
            this._disposeTokenSource.Dispose();
        }

        private void OnMakingRequest(HttpRequestEventArgs e)
        {
            this.MakingHttpRequest?.Invoke(this, e);
        }
    }
}