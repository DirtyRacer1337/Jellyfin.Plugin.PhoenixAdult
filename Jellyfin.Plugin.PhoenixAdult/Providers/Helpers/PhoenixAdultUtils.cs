using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using MediaBrowser.Model.Providers;
using PhoenixAdult;
using SkiaSharp;
using PhoenixAdult.Helpers;
using System.Net.Http;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;

#if __EMBY__
using MediaBrowser.Model.Logging;
#else
using Microsoft.Extensions.Logging;
#endif

namespace PhoenixAdult.Helpers
{
    internal static class HTTP
    {
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
                request._method = HttpMethod.Get;

            Logger.Info(string.Format(CultureInfo.InvariantCulture, "Requesting {1} \"{0}\"", request._url, request._method.Method));

            var http = PhoenixAdultProvider.FlurlHttp;
            http.BaseUrl = request._url;
            http.Headers.Clear();
            http.Cookies.Clear();

            http.WithHeader("User-Agent", GetUserAgent());

            if (request._headers != null)
            {
                http.WithHeaders(request._headers);
            }

            if (request._cookies != null)
            {
                http.WithCookies(request._cookies);
            }

            var data = http.Request();

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

            result._cookies = http.Cookies;

            return result;
        }
    }

    internal static class HTML
    {
        public static async Task<HtmlNode> ElementFromURL(string url, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
        {
            HtmlNode html = new HtmlDocument().DocumentNode;
            var http = await HTTP.Request(new HTTP.HTTPRequest
            {
                _url = url,
                _headers = headers,
                _cookies = cookies,
            }, cancellationToken).ConfigureAwait(false);
            if (http._response.IsSuccessStatusCode)
                html = ElementFromStream(await http._response.Content.ReadAsStreamAsync().ConfigureAwait(false));

            return html;
        }

        public static HtmlNode ElementFromString(string data)
        {
            HtmlNode html;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                html = ElementFromStream(stream);
            }

            return html;
        }

        public static HtmlNode ElementFromStream(Stream data)
        {
            var html = new HtmlDocument();
            html.Load(data);

            return html.DocumentNode;
        }
    }
}

public static class StringExtensions
{
    public static bool Contains(this string source, string toCheck, StringComparison stringComparison) => source?.IndexOf(toCheck, stringComparison) >= 0;

    public static string Replace(this string source, string from, string to, int nums, StringComparison stringComparison)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(from))
            return source;

        for (var i = 0; i < nums; i++)
        {
            int pos = source.IndexOf(from, stringComparison);
            if (pos < 0)
                return source;

            source = source.Substring(0, pos) + to + source.Substring(pos + from.Length);
        }

        return source;
    }

    public static string Replace(this string source, string from, string to, StringComparison stringComparison)
    {
        if (stringComparison == StringComparison.OrdinalIgnoreCase)
            return Regex.Replace(source, Regex.Escape(from), to, RegexOptions.IgnoreCase);
        else
            return source?.Replace(from, to);
    }
}

internal static class Base58
{
    private const string DIGITS = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static string EncodePlain(byte[] data)
    {
        if (data == null)
            return null;

        var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

        var result = string.Empty;
        while (intData > 0)
        {
            var remainder = (int)(intData % 58);
            intData /= 58;
            result = DIGITS[remainder] + result;
        }

        for (var i = 0; i < data.Length && data[i] == 0; i++)
            result = '1' + result;

        return result;
    }

    public static byte[] DecodePlain(string data)
    {
        if (data == null)
            return null;

        BigInteger intData = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var digit = DIGITS.IndexOf(data[i]);

            if (digit < 0)
                throw new FormatException($"Invalid Base58 character `{data[i]}` at position {i}");

            intData = intData * 58 + digit;
        }

        var leadingZeroCount = data.TakeWhile(c => c == '1').Count();
        var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
        var bytesWithoutLeadingZeros = intData.ToByteArray().Reverse().SkipWhile(b => b == 0);
        var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();

        return result;
    }
}

internal static class LevenshteinDistance
{
    public static int Calculate(string source1, string source2)
    {
        if (source1 == null || source2 == null)
            return -1;

        var source1Length = source1.Length;
        var source2Length = source2.Length;

        var matrix = new int[source1Length + 1, source2Length + 1];

        if (source1Length == 0)
            return source2Length;

        if (source2Length == 0)
            return source1Length;

        for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
        for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

        for (var i = 1; i <= source1Length; i++)
        {
            for (var j = 1; j <= source2Length; j++)
            {
                var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source1Length, source2Length];
    }
}

internal static class ImageHelper
{
    public static async Task<RemoteImageInfo> GetImageSizeAndValidate(RemoteImageInfo item, CancellationToken cancellationToken)
    {
        var http = await HTTP.Request(new HTTP.HTTPRequest
        {
            _url = item.Url,
            _method = HttpMethod.Head,
        }, cancellationToken).ConfigureAwait(false);
        if (http._response.IsSuccessStatusCode)
        {
            var httpStream = await HTTP.Request(new HTTP.HTTPRequest
            {
                _url = item.Url,
            }, cancellationToken).ConfigureAwait(false);
            using (var img = SKBitmap.Decode(await httpStream._response.Content.ReadAsStreamAsync().ConfigureAwait(false)))
            {
                if (img.Width > 100)
                    return new RemoteImageInfo
                    {
                        ProviderName = item.ProviderName,
                        Url = item.Url,
                        Type = item.Type,
                        Height = img.Height,
                        Width = img.Width
                    };
            }
        }

        return null;
    }
}

internal static class Logger
{
    private static ILogger Log { get; } = PhoenixAdultProvider.Log;
    public static void Info(string text)
    {
#if __EMBY__
        Log.Info(text);
#else
        Log.LogInformation(text);
#endif
    }

    public static void Error(string text)
    {
#if __EMBY__
        Log.Error(text);
#else
        Log.LogError(text);
#endif
    }

    public static void Debug(string text)
    {
#if __EMBY__
        Log.Debug(text);
#else
        Log.LogDebug(text);
#endif
    }

    public static void Warning(string text)
    {
#if __EMBY__
        Log.Warn(text);
#else
        Log.LogWarning(text);
#endif
    }
}

internal static class Database
{
    private const string BaseURL = "https://raw.githubusercontent.com/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/database/data/";

    private static readonly string _databasePath = Path.Combine(Plugin.Instance.DataFolderPath, "data");

    private static readonly string[] _databaseFiles = { "SiteList.json", "Actors.json", "Genres.json" };

    public static SiteListStructure SiteList;

    public static ActorsStructure Actors;

    public static GenresStructure Genres;

    public struct SiteListStructure
    {
        public Dictionary<string, string> Abbrieviations { get; set; }
    }

    public struct ActorsStructure
    {
        public Dictionary<string, string[]> ActorsReplace { get; set; }

        public Dictionary<int, string[]> ActorsStudioIndexes { get; set; }

        public Dictionary<int, Dictionary<string, string[]>> ActorsReplaceStudios { get; set; }
    }

    public struct GenresStructure
    {
        public Dictionary<string, string[]> GenresReplace { get; set; }

        public List<string> GenresSkip { get; set; }

        public List<string> GenresPartialSkip { get; set; }
    }

    public static async void Load(CancellationToken cancellationToken)
    {
        foreach (var fileName in _databaseFiles)
        {
            var http = await HTTP.Request(new HTTP.HTTPRequest
            {
                _url = BaseURL + fileName
            }, cancellationToken).ConfigureAwait(false);
            if (http._response.IsSuccessStatusCode)
            {
                Logger.Info($"Database file \"{fileName}\" downloaded successfully");
                var data = await http._response.Content.ReadAsStringAsync().ConfigureAwait(false);
                File.WriteAllText(Path.Combine(_databasePath, fileName), data);
            }
        }

        Update();
    }

    public static void Update()
    {
        foreach (var fileName in _databaseFiles)
        {
            var filePath = Path.Combine(_databasePath, fileName);
            if (File.Exists(filePath))
            {
                var data = File.ReadAllText(filePath, Encoding.UTF8);
                switch (fileName)
                {
                    case "SiteList.json":
                        SiteList = JsonConvert.DeserializeObject<SiteListStructure>(data);
                        break;
                    case "Actors.json":
                        Actors = JsonConvert.DeserializeObject<ActorsStructure>(data);
                        break;
                    case "Genres.json":
                        Genres = JsonConvert.DeserializeObject<GenresStructure>(data);
                        break;
                }
            }
        }

        Logger.Info($"Database loading finished");
    }
}
