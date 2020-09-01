using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using HtmlAgilityPack;

namespace PhoenixAdult.Providers.Helpers
{
    internal static class PhoenixAdultHelper
    {
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

        public static string Encode(string text)
            => Base58.EncodePlain(Encoding.UTF8.GetBytes(text));

        public static string Decode(string base64Text)
            => Encoding.UTF8.GetString(Base58.DecodePlain(base64Text));

        public static async Task<List<string>> GetGoogleSearchResults(string text, int[] siteNum, CancellationToken cancellationToken)
        {
            var results = new List<string>();
            string searchTerm;

            if (siteNum != null)
            {
                var site = GetSearchBaseURL(siteNum).Split(':')[1].Replace("//", "", StringComparison.OrdinalIgnoreCase);
                searchTerm = $"site:{site} {text}";
            }
            else
                searchTerm = text;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(searchTerm);
                await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

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
    }
}
