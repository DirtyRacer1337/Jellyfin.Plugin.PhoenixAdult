using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class GoogleSearch
    {
        public static async Task<List<string>> GetSearchResults(string text, int[] siteNum, CancellationToken cancellationToken)
        {
            var results = new List<string>();
            string searchTerm;

            if (siteNum != null)
            {
                var site = new Uri(Helper.GetSearchBaseURL(siteNum)).Host;
                searchTerm = $"site:{site} {text}";
            }
            else
            {
                searchTerm = text;
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var url = "https://www.google.com/search?q=" + searchTerm;
                var html = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = html.SelectNodesSafe("//a[@href]");
                foreach (var searchResult in searchResults)
                {
                    var searchURL = WebUtility.HtmlDecode(searchResult.Attributes["href"].Value);
                    if (searchURL.StartsWith("/url", StringComparison.OrdinalIgnoreCase))
                    {
                        searchURL = HttpUtility.ParseQueryString(searchURL.Replace("/url", string.Empty, StringComparison.OrdinalIgnoreCase))["q"];
                    }

                    if (searchURL.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !searchURL.Contains("google", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(searchURL);
                    }
                }
            }

            return results;
        }

        public static async Task<List<string>> GetSearchResults(string text, CancellationToken cancellationToken)
            => await GetSearchResults(text, null, cancellationToken).ConfigureAwait(false);
    }
}
