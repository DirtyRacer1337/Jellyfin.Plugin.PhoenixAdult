using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class HTTP
    {
        static HTTP()
        {
            FlurlHTTP.Configure(settings => settings.Timeout = TimeSpan.FromSeconds(120));
            FlurlHTTP.Configure(settings => settings.Redirects.AllowSecureToInsecure = true);
            FlurlHTTP.Configure(settings => settings.Redirects.ForwardAuthorizationHeader = true);
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

            var data = FlurlHTTP.AllowAnyHttpStatus().Request();
            data = data.WithHeader("User-Agent", GetUserAgent());

            if (request.Headers != null)
            {
                data = data.WithHeaders(request.Headers);
            }

            if (request.Cookies != null)
            {
                data = data.WithCookies(request.Cookies);
            }

            data = data.WithAutoRedirect(request.AutoRedirect);

            Task<IFlurlResponse> responseTask = null;
            switch (request.Method.Method)
            {
                case "GET":
                    responseTask = data.GetAsync(cancellationToken);
                    break;

                case "POST":
                    responseTask = data.PostStringAsync(request.Param, cancellationToken);
                    break;

                case "HEAD":
                    responseTask = data.HeadAsync(cancellationToken);
                    break;

                default:
                    Logger.Error($"Method {request.Method.Method} not implemented");
                    break;
            }

            IFlurlResponse response = null;
            if (responseTask != null)
            {
                try
                {
                    response = await responseTask.ConfigureAwait(false);
                }
                catch (FlurlHttpException e)
                {
                    Logger.Error(e.Message);
                }
            }

            if (response != null)
            {
                var cookies = new List<Cookie>();
                foreach (var cookie in response.Cookies)
                {
                    cookies.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                }

                result.Cookies = cookies;
                result.Content = await response.ResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.ContentStream = await response.ResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                result.IsOK = response.ResponseMessage.IsSuccessStatusCode;
                result.Headers = response.ResponseMessage.Headers;
            }

            return result;
        }

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null, bool redirect = true)
            => await Request(url, CreateRequest(method, headers, cookies, redirect), cancellationToken).ConfigureAwait(false);

        public static async Task<HTTPResponse> Request(string url, CancellationToken cancellationToken, bool redirect = true)
            => await Request(url, null, cancellationToken, null, null, redirect).ConfigureAwait(false);

        public static HTTPRequest CreateRequest(HttpMethod method, string param, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null, bool redirect = true)
        {
            return new HTTPRequest
            {
                Method = method,
                Headers = headers,
                Cookies = cookies,
                Param = param,
                AutoRedirect = redirect,
            };
        }

        public static HTTPRequest CreateRequest(HttpMethod method, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null, bool redirect = true)
        {
            return CreateRequest(method, null, headers, cookies, redirect);
        }

        public static HTTPRequest CreateRequest(IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null, bool redirect = true)
        {
            return CreateRequest(null, null, headers, cookies, redirect);
        }

        public static HTTPRequest CreateRequest(string param, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null, bool redirect = true)
        {
            return CreateRequest(null, param, headers, cookies, redirect);
        }

        internal struct HTTPResponse
        {
            public string Content { get; set; }

            public Stream ContentStream { get; set; }

            public bool IsOK { get; set; }

            public IList<Cookie> Cookies { get; set; }

            public HttpResponseHeaders Headers { get; set; }
        }

        internal struct HTTPRequest
        {
            public HttpMethod Method { get; set; }

            public string Param { get; set; }

            public IDictionary<string, string> Headers { get; set; }

            public IDictionary<string, string> Cookies { get; set; }

            public bool AutoRedirect { get; set; }
        }
    }
}
