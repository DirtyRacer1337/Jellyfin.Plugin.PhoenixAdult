using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public class PhoenixAdultProvider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => "PhoenixAdult";

        public static string PluginName;
        public static ILogger Log;
        public static IHttpClient Http;

        public PhoenixAdultProvider(ILoggerFactory log, IHttpClient http)
        {
            PluginName = Name;
            if (log != null)
                Log = log.CreateLogger(Name);
            Http = http;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            List<RemoteSearchResult> result = new List<RemoteSearchResult>();

            if (searchInfo == null)
                return result;

            var site = GetSiteFromTitle(searchInfo.Name);
            if (site.Key != null)
            {
                string searchTitle = GetClearTitle(searchInfo.Name, site.Value),
                       encodedTitle,
                       searchDate;
                var titleAfterDate = GetDateFromTitle(searchTitle);

                var siteNum = new int[2] {
                    site.Key[0],
                    site.Key[1]
                };
                searchTitle = titleAfterDate[0];
                searchDate = titleAfterDate[1];
                encodedTitle = HttpUtility.UrlEncode(searchTitle);

                Log.LogInformation($"siteNum: {siteNum[0]}:{siteNum[1]}");
                Log.LogInformation($"searchTitle: {searchTitle}");
                Log.LogInformation($"encodedTitle: {encodedTitle}");
                Log.LogInformation($"searchDate: {searchDate}");

                var provider = PhoenixAdultList.GetProviderBySiteID(siteNum[0]);
                if (provider != null)
                {
                    result = await provider.Search(siteNum, searchTitle, encodedTitle, searchDate, cancellationToken).ConfigureAwait(false);
                    if (result.Count > 0)
                        if (result.Any(scene => scene.IndexNumber.HasValue))
                            result = result.OrderByDescending(scene => scene.IndexNumber.HasValue).ThenBy(scene => scene.IndexNumber).ToList();
                        else if (DateTime.TryParseExact(searchDate, "yyyy-MM-dd", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime searchDateObj) && result.All(scene => scene.PremiereDate.HasValue))
                            result = result.OrderByDescending(scene => DateTime.Compare(searchDateObj, (DateTime)scene.PremiereDate) == 0).ToList();
                        else
                            result = result.OrderByDescending(scene => 100 - PhoenixAdultHelper.LevenshteinDistance(searchTitle, scene.Name)).ToList();
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>
            {
                HasMetadata = false,
                Item = new Movie()
            };

            if (info == null)
                return result;

            var sceneID = info.ProviderIds;
            if (!sceneID.ContainsKey(Name))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                    sceneID = searchResults.First().ProviderIds;
            }

            var externalID = sceneID.GetValueOrDefault(Name);
            if (string.IsNullOrEmpty(externalID))
                return result;

            var curID = externalID.Split('#');
            if (curID.Length < 3)
                return result;

            var provider = PhoenixAdultList.GetProviderBySiteID(int.Parse(curID[0], PhoenixAdultHelper.Lang));
            if (provider != null)
            {
                result = await provider.Update(curID, cancellationToken).ConfigureAwait(false);
                result.HasMetadata = true;
                result.Item.OfficialRating = "XXX";
                result.Item.ProviderIds = sceneID;

                if ((result.People != null) && result.People.Any())
                    result.People = PhoenixAdultPeoples.Cleanup(result);
                if (result.Item.Genres != null && result.Item.Genres.Any())
                    result.Item.Genres = PhoenixAdultGenres.Cleanup(result.Item.Genres, result.Item.Name);
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultHelper.GetImageResponse(url, cancellationToken);

        public static KeyValuePair<int[], string> GetSiteFromTitle(string title)
        {
            string clearName = Regex.Replace(title, @"\W", string.Empty);
            var possibleSites = new Dictionary<int[], string>();

            foreach (var site in PhoenixAdultList.SiteList)
                foreach (var siteData in site.Value)
                {
                    string clearSite = Regex.Replace(siteData.Value[0], @"\W", string.Empty);
                    if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                        possibleSites.Add(new int[] { site.Key, siteData.Key }, clearSite);
                }

            if (possibleSites.Count > 0)
                return possibleSites.OrderByDescending(x => x.Value.Length).First();

            return new KeyValuePair<int[], string>(null, null);
        }

        public static string GetClearTitle(string title, string siteName)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            string clearName = PhoenixAdultHelper.Lang.TextInfo.ToTitleCase(title),
                   clearSite = siteName;

            clearName = clearName.Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

            clearName = Regex.Replace(clearName, @"[^a-zA-Z0-9 ]", " ");
            clearSite = Regex.Replace(clearSite, @"\W", string.Empty);

            bool matched = false;
            while (clearName.Contains(' ', StringComparison.OrdinalIgnoreCase))
            {
                clearName = PhoenixAdultHelper.ReplaceFirst(clearName, " ", string.Empty);
                if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                clearName = clearName.Replace(clearSite, string.Empty, StringComparison.OrdinalIgnoreCase);
                clearName = string.Join(" ", clearName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }


            return clearName;
        }

        public static string[] GetDateFromTitle(string title)
        {
            string searchDate,
                   searchTitle = title;
            var regExRules = new Dictionary<string, string> {
                { @"\b\d{4} \d{2} \d{2}\b", "yyyy MM dd" },
                { @"\b\d{2} \d{2} \d{2}\b", "yy MM dd" }
            };
            string[] searchData = new string[2] { searchTitle, string.Empty };

            foreach (var regExRule in regExRules)
            {
                var regEx = Regex.Match(searchTitle, regExRule.Key);
                if (regEx.Groups.Count > 0)
                    if (DateTime.TryParseExact(regEx.Groups[0].Value, regExRule.Value, PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime date))
                    {
                        searchDate = date.ToString("yyyy-MM-dd", PhoenixAdultHelper.Lang);
                        searchTitle = Regex.Replace(searchTitle, regExRule.Key, string.Empty).Trim();

                        searchData = new string[2] { searchTitle, searchDate };
                        break;
                    }
            }

            return searchData;
        }
    }
}
