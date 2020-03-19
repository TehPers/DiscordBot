using System;
using System.Net.Http;

namespace Warframe
{
    public class HttpRequestEventArgs : EventArgs
    {
        public Uri Uri { get; }
        public HttpMethod Method { get; }

        public HttpRequestEventArgs(Uri uri, HttpMethod method)
        {
            this.Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.Method = method ?? throw new ArgumentNullException(nameof(method));
        }
    }
}