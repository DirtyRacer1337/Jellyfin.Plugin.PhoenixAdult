using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using PhoenixAdult;

internal static class HTTP
{
    private static readonly FlurlClient _http = PhoenixAdultProvider.FlurlHttp;

    public struct HTTPRequest
    {
        public string _url;
        public HttpMethod _method;
        public string _param;
        public IDictionary<string, string> _headers;
        public IDictionary<string, string> _cookies;
    }

    public struct HTTPResponse
    {
        public HttpResponseMessage _response;
        public IDictionary<string, Cookie> _cookies;
    }
    public static string GetUserAgent() => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";

    public static async Task<HTTPResponse> Request(HTTPRequest request, CancellationToken cancellationToken)
    {
        HTTPResponse result = new HTTPResponse();

        request._url = Uri.EscapeUriString(request._url);

        if (request._method == null)
        {
            if (!string.IsNullOrEmpty(request._param))
            {
                request._method = HttpMethod.Post;
            }
            else
            {
                request._method = HttpMethod.Get;
            }
        }

        Logger.Info(string.Format(CultureInfo.InvariantCulture, "Requesting {1} \"{0}\"", request._url, request._method.Method));

        _http.BaseUrl = request._url;
        _http.Headers.Clear();
        _http.Cookies.Clear();

        _http.WithHeader("User-Agent", GetUserAgent());

        if (request._headers != null)
        {
            _http.WithHeaders(request._headers);
        }

        if (request._cookies != null)
        {
            _http.WithCookies(request._cookies);
        }

        var data = _http.Request();

        try
        {
            switch (request._method.Method)
            {
                case "GET":
                    result._response = await data.GetAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case "POST":
                    result._response = await data.PostStringAsync(request._param, cancellationToken).ConfigureAwait(false);
                    break;
                case "HEAD":
                    result._response = await data.HeadAsync(cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    return result;
            }

        }
        catch (FlurlHttpTimeoutException e)
        {
            Logger.Error(e.Message);
            return new HTTPResponse
            {
                _response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.RequestTimeout
                }
            };
        }

        result._cookies = _http.Cookies;

        return result;
    }
}
