using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PhoenixAdult.Helpers
{
    class GoogleSearch
    {
        public static async Task<List<string>> GetSearchResults(string text, int[] siteNum, CancellationToken cancellationToken)
        {
            var results = new List<string>();
            string searchTerm;

            if (siteNum != null)
            {
                var site = PhoenixAdultHelper.GetSearchBaseURL(siteNum).Split(':')[1].Replace("//", "", StringComparison.OrdinalIgnoreCase);
                searchTerm = $"site:{site} {text}";
            }
            else
                searchTerm = text;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(searchTerm);
                var html = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = html.SelectNodes("//a");
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

        public static async Task<List<string>> GetSearchResults(string text, CancellationToken cancellationToken)
            => await GetSearchResults(text, null, cancellationToken).ConfigureAwait(false);
    }
}
