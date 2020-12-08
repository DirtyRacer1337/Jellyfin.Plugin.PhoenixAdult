using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class HTTP
    {
        static HTTP()
        {
            Http.Timeout = TimeSpan.FromSeconds(120);
        }

        private static CookieContainer CookieContainer { get; } = new CookieContainer();

        private static HttpClientHandler HttpHandler { get; } = new HttpClientHandler() { CookieContainer = CookieContainer };

        private static HttpClient Http { get; } = new HttpClient(HttpHandler);

        public static string GetUserAgent()
            => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, HttpContent param, IDictionary<string, string> headers, IDictionary<string, string> cookies, CancellationToken cancellationToken)
        {
            var result = new HTTPResponse()
            {
                IsOK = false,
            };

            url = Uri.EscapeUriString(Uri.UnescapeDataString(url));

            if (method == null)
            {
                method = HttpMethod.Get;
            }

            var request = new HttpRequestMessage(method, new Uri(url));

            Logger.Debug(string.Format(CultureInfo.InvariantCulture, "Requesting {1} \"{0}\"", request.RequestUri.AbsoluteUri, method.Method));

            request.Headers.TryAddWithoutValidation("User-Agent", GetUserAgent());

            if (param != null)
            {
                request.Content = param;
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (cookies != null)
            {
                foreach (var cookie in cookies)
                {
                    CookieContainer.Add(request.RequestUri, new Cookie(cookie.Key, cookie.Value));
                }
            }

            HttpResponseMessage response = null;
            try
            {
                response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                Logger.Error(e.Message);
            }

            if (response != null)
            {
                result.IsOK = response.IsSuccessStatusCode;
                result.Content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.ContentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                result.Headers = response.Headers;
                result.Cookies = CookieContainer.GetCookies(request.RequestUri).Cast<Cookie>();
            }

            return result;
        }

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, HttpContent param, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, method, param, headers, cookies, cancellationToken).ConfigureAwait(false);

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, method, null, headers, cookies, cancellationToken).ConfigureAwait(false);

        public static async Task<HTTPResponse> Request(string url, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, null, null, headers, cookies, cancellationToken).ConfigureAwait(false);

        internal struct HTTPResponse
        {
            public string Content { get; set; }

            public Stream ContentStream { get; set; }

            public bool IsOK { get; set; }

            public IEnumerable<Cookie> Cookies { get; set; }

            public HttpResponseHeaders Headers { get; set; }
        }
    }
}
