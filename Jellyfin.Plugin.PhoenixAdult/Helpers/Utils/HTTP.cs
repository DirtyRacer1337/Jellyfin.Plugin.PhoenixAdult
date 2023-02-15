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
using FlareSolverrSharp;
using Microsoft.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.InMemory;
using MihaZupan;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class HTTP
    {
        static HTTP()
        {
            CloudflareHandler = new ClearanceHandler(Plugin.Instance.Configuration.FlareSolverrURL)
            {
                MaxTimeout = (int)TimeSpan.FromSeconds(120).TotalMilliseconds,
            };

            if (Plugin.Instance.Configuration.ProxyEnable && !string.IsNullOrEmpty(Plugin.Instance.Configuration.ProxyHost) && Plugin.Instance.Configuration.ProxyPort > 0)
            {
                Logger.Info("Proxy Enabled");
                var proxy = new List<ProxyInfo>();

                if (string.IsNullOrEmpty(Plugin.Instance.Configuration.ProxyLogin) || string.IsNullOrEmpty(Plugin.Instance.Configuration.ProxyPassword))
                {
                    proxy.Add(new ProxyInfo(Plugin.Instance.Configuration.ProxyHost, Plugin.Instance.Configuration.ProxyPort));
                    CloudflareHandler.ProxyUrl = $"socks5://{Plugin.Instance.Configuration.ProxyHost}:{Plugin.Instance.Configuration.ProxyPort}";
                }
                else
                {
                    proxy.Add(new ProxyInfo(
                        Plugin.Instance.Configuration.ProxyHost,
                        Plugin.Instance.Configuration.ProxyPort,
                        Plugin.Instance.Configuration.ProxyLogin,
                        Plugin.Instance.Configuration.ProxyPassword));
                }

                Proxy = new HttpToSocks5Proxy(proxy.ToArray());
            }

            HttpHandler = new HttpClientHandler()
            {
                CookieContainer = CookieContainer,
                Proxy = Proxy,
            };

            if (Plugin.Instance.Configuration.DisableSSLCheck)
            {
                HttpHandler.ServerCertificateCustomValidationCallback += (sender, certificate, chain, errors) => true;
            }

            if (!Plugin.Instance.Configuration.DisableCaching)
            {
                Logger.Debug("Caching Enabled");
                CacheHandler = new InMemoryCacheHandler(HttpHandler, CacheExpirationProvider.CreateSimple(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)));
                CloudflareHandler.InnerHandler = CacheHandler;
            }
            else
            {
                Logger.Debug("Caching Disabled");
                CloudflareHandler.InnerHandler = HttpHandler;
            }

            Http = new HttpClient(CloudflareHandler)
            {
                Timeout = TimeSpan.FromSeconds(120),
            };
        }

        private static CookieContainer CookieContainer { get; } = new CookieContainer();

        private static IWebProxy Proxy { get; set; }

        private static HttpClientHandler HttpHandler { get; set; }

        private static InMemoryCacheHandler CacheHandler { get; set; }

        private static ClearanceHandler CloudflareHandler { get; set; }

        private static HttpClient Http { get; set; }

        public static string GetUserAgent()
            => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36";

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

            if (CacheHandler != null && request.RequestUri.AbsoluteUri == Consts.DatabaseUpdateURL)
            {
                CacheHandler.InvalidateCache(request.RequestUri);
            }

            HttpResponseMessage response = null;
            try
            {
                response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Request error: {e.Message}");

                await Analytics.Send(
                    new AnalyticsExeption
                    {
                        Request = url,
                        Exception = e,
                    }, cancellationToken).ConfigureAwait(false);
            }

            if (response != null)
            {
                result.IsOK = response.IsSuccessStatusCode;
#if __EMBY__
                result.Content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.ContentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
                result.Content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                result.ContentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
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
