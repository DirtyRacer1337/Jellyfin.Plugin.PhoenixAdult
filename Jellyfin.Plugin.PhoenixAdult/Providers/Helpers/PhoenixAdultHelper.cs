using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using HtmlAgilityPack;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Helpers
{
    public static class PhoenixAdultHelper
    {
        public static readonly CultureInfo Lang = new CultureInfo("en-US", false);

        public static string GetSearchSiteName(int[] siteNum)
        {
            if (siteNum == null)
                return null;

            return PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]][0];
        }

        public static string GetSearchBaseURL(int[] siteNum)
        {
            if (siteNum == null)
                return null;

            if (!string.IsNullOrEmpty(PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]].ElementAtOrDefault(1)))
                return PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]][1];
            else
                return PhoenixAdultList.SiteList[siteNum[0]].First().Value[1];
        }

        public static string GetSearchSearchURL(int[] siteNum)
        {
            if (siteNum == null)
                return null;

            if (!string.IsNullOrEmpty(PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]].ElementAtOrDefault(2)))
                return PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]][2];
            else
                return PhoenixAdultList.SiteList[siteNum[0]].First().Value[2];
        }

        public static int LevenshteinDistance(string source1, string source2)
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

        public static string Encode(string text)
            => Base58.EncodePlain(Encoding.UTF8.GetBytes(text));

        public static string Decode(string base64Text)
            => Encoding.UTF8.GetString(Base58.DecodePlain(base64Text));

        public static string ReplaceFirst(string text, string search, string replace)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search))
                return text;

            int pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
                return text;

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static async Task<List<string>> GetGoogleSearchResults(string text, int[] siteNum, CancellationToken cancellationToken)
        {
            var results = new List<string>();
            string searchTerm;

            if (siteNum != null)
            {
                var site = GetSearchBaseURL(siteNum).Split("://")[1];
                searchTerm = $"site:{site} {text}";
            }
            else
                searchTerm = text;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                PhoenixAdultProvider.Log.LogInformation($"Using Google Search '{searchTerm}'");
                var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(searchTerm);
                var http = await url.GetAsync(cancellationToken).ConfigureAwait(false);
                var html = new HtmlDocument();
                html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

                var searchResults = html.DocumentNode.SelectNodes("//a");
                if (searchResults != null)
                    foreach (var searchResult in searchResults)
                    {
                        var searchURL = WebUtility.HtmlDecode(searchResult.Attributes["href"].Value);
                        if (searchURL.StartsWith("/url", StringComparison.OrdinalIgnoreCase))
                        {
                            searchURL = HttpUtility.ParseQueryString(searchURL.Replace("/url", "", StringComparison.OrdinalIgnoreCase))["q"];

                            if (searchURL.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !searchURL.Contains("google", StringComparison.OrdinalIgnoreCase))
                                results.Add(searchURL);
                        }
                    }
            }

            return results;
        }

        public static async Task<List<string>> GetGoogleSearchResults(string text, CancellationToken cancellationToken)
            => await GetGoogleSearchResults(text, null, cancellationToken).ConfigureAwait(false);

        public static Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
            => PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
    }
}

public static class Base58
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
            var digit = DIGITS.IndexOf(data[i], StringComparison.Ordinal);

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
