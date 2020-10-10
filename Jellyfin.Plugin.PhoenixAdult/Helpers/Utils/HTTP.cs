using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class HTTP
    {
        static HTTP()
        {
            FlurlHTTP.AllowAnyHttpStatus().EnableCookies();
            FlurlHTTP.Configure(settings => settings.Timeout = TimeSpan.FromSeconds(120));
        }

        private static FlurlClient FlurlHTTP { get; } = new FlurlClient();

        public static string GetUserAgent()
            => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";

        public static async Task<HTTPResponse> Request(string url, HTTPRequest request, CancellationToken cancellationToken)
        {
            HTTPResponse result = new HTTPResponse()
            {
                IsOK = false,
            };

            url = Uri.EscapeUriString(Uri.UnescapeDataString(url));

            if (request.Method == null)
            {
                if (!string.IsNullOrEmpty(request.Param))
                {
                    request.Method = HttpMethod.Post;
                }
                else
                {
                    request.Method = HttpMethod.Get;
                }
            }

            Logger.Debug(string.Format(CultureInfo.InvariantCulture, "Requesting {1} \"{0}\"", url, request.Method.Method));

            FlurlHTTP.BaseUrl = url;
            FlurlHTTP.Headers.Clear();
            FlurlHTTP.Cookies.Clear();

            FlurlHTTP.WithHeader("User-Agent", GetUserAgent());

            if (request.Headers != null)
            {
                FlurlHTTP.WithHeaders(request.Headers);
            }

            if (request.Cookies != null)
            {
                FlurlHTTP.WithCookies(request.Cookies);
            }

            var data = FlurlHTTP.Request();

            HttpResponseMessage response = null;
            try
            {
                switch (request.Method.Method)
                {
                    case "GET":
                        response = await data.GetAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    case "POST":
                        response = await data.PostStringAsync(request.Param, cancellationToken).ConfigureAwait(false);
                        break;

                    case "HEAD":
                        response = await data.HeadAsync(cancellationToken).ConfigureAwait(false);
                        break;

                    default:
                        Logger.Error($"Method {request.Method.Method} not implemented");
                        break;
                }
            }
            catch (FlurlHttpException e)
            {
                Logger.Error(e.Message);
            }

            if (response != null)
            {
                result.Cookies = FlurlHTTP.Cookies;
                result.Content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.ContentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                result.IsOK = response.IsSuccessStatusCode;
            }

            return result;
        }

        public static async Task<HTTPResponse> Request(string url, CancellationToken cancellationToken)
            => await Request(url, null, cancellationToken).ConfigureAwait(false);

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, HTTP.CreateRequest(method, headers, cookies), cancellationToken).ConfigureAwait(false);

        public static HTTPRequest CreateRequest(HttpMethod method, string param, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
        {
            return new HTTPRequest
            {
                Method = method,
                Headers = headers,
                Cookies = cookies,
                Param = param,
            };
        }

        public static HTTPRequest CreateRequest(HttpMethod method, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
        {
            return CreateRequest(method, null, headers, cookies);
        }

        public static HTTPRequest CreateRequest(IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
        {
            return CreateRequest(null, null, headers, cookies);
        }

        public static HTTPRequest CreateRequest(string param, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
        {
            return CreateRequest(null, param, headers, cookies);
        }

        internal struct HTTPResponse
        {
            public string Content { get; set; }

            public Stream ContentStream { get; set; }

            public bool IsOK { get; set; }

            public IDictionary<string, Cookie> Cookies { get; set; }
        }

        internal struct HTTPRequest
        {
            public HttpMethod Method { get; set; }

            public string Param { get; set; }

            public IDictionary<string, string> Headers { get; set; }

            public IDictionary<string, string> Cookies { get; set; }
        }
    }
}
