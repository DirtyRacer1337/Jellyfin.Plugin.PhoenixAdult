using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Providers;
using PhoenixAdult.Providers.Helpers;

#if __EMBY__
using MediaBrowser.Model.Logging;
#else
using Microsoft.Extensions.Logging;
#endif

namespace PhoenixAdult
{
    public class PhoenixAdultProvider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => Plugin.Instance.Name;

#if __EMBY__
        public static ILogger Log { get; set; }
#else
        public static ILogger<PhoenixAdultProvider> Log { get; set; }
#endif
        public static IHttpClient Http { get; set; }
        public static CultureInfo Lang { get; } = new CultureInfo("en-US", false);

        public PhoenixAdultProvider(
#if __EMBY__
        ILogManager logger,
#else
        ILogger<PhoenixAdultProvider> logger,
#endif
            IHttpClient http)
        {
#if __EMBY__
            if (logger != null)
                Log = logger.GetLogger(Name);
#else
            Log = logger;
#endif
            Http = http;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            List<RemoteSearchResult> result = new List<RemoteSearchResult>();

            if (searchInfo == null)
                return result;

            if (!Plugin.Instance.Configuration.IgnoreYearWarning)
                if (searchInfo.Year.HasValue)
                {
                    Logger.Info("Year detected (probably important data was stripped), required manual identify");
                    return result;
                }

            Logger.Info($"searchInfo.Name: {searchInfo.Name}");

            var title = ReplaceAbbrieviation(searchInfo.Name);
            var site = GetSiteFromTitle(title);
            if (site.Key != null)
            {
                string searchTitle = GetClearTitle(title, site.Value),
                       searchDate = string.Empty,
                       encodedTitle;
                DateTime? searchDateObj;
                var titleAfterDate = GetDateFromTitle(searchTitle);

                var siteNum = new int[2] {
                    site.Key[0],
                    site.Key[1]
                };
                searchTitle = titleAfterDate.Item1;
                searchDateObj = titleAfterDate.Item2;
                if (searchDateObj.HasValue)
                    searchDate = searchDateObj.Value.ToString("yyyy-MM-dd", Lang);
                else
                {
                    if (searchInfo.PremiereDate.HasValue)
                        searchDate = searchInfo.PremiereDate.Value.ToString("yyyy-MM-dd", Lang);
                }
                encodedTitle = Uri.EscapeDataString(searchTitle);

                Logger.Info($"site: {siteNum[0]}:{siteNum[1]} ({site.Value})");
                Logger.Info($"searchTitle: {searchTitle}");
                Logger.Info($"encodedTitle: {encodedTitle}");
                Logger.Info($"searchDate: {searchDate}");

                var provider = PhoenixAdultList.GetProviderBySiteID(siteNum[0]);
                if (provider != null)
                {
                    Logger.Info($"provider: {provider}");
                    try
                    {
                        result = await provider.Search(siteNum, searchTitle, encodedTitle, searchDateObj, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.ToString());
                    }
                    finally
                    {
                        if (result.Any())
                            if (result.Any(scene => scene.IndexNumber.HasValue))
                                result = result.OrderByDescending(scene => scene.IndexNumber.HasValue).ThenByDescending(scene => scene.IndexNumber).ToList();
                            else if (!string.IsNullOrEmpty(searchDate) && result.All(scene => scene.PremiereDate.HasValue) && result.Any(scene => scene.PremiereDate.Value != searchDateObj))
                                result = result.OrderBy(scene => Math.Abs((searchDateObj - scene.PremiereDate).Value.TotalDays)).ToList();
                            else
                                result = result.OrderByDescending(scene => 100 - LevenshteinDistance.Calculate(searchTitle, scene.Name)).ToList();
                    }
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

            if (!sceneID.TryGetValue(Name, out string externalID))
                return result;

            var curID = externalID.Split('#');
            if (curID.Length < 3)
                return result;

            var provider = PhoenixAdultList.GetProviderBySiteID(int.Parse(curID[0], Lang));
            if (provider != null)
            {
                Logger.Info($"PhoenixAdult ID: {externalID}");
                try
                {
                    result = await provider.Update(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
                finally
                {
                    if (result != null)
                    {
                        result.HasMetadata = true;
                        result.Item.OfficialRating = "XXX";
                        result.Item.ProviderIds = sceneID;

                        if (result.Item.PremiereDate.HasValue)
                            result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;

                        if ((result.People != null) && result.People.Any())
                            result.People = PhoenixAdultActors.Cleanup(result);
                        if (result.Item.Genres != null && result.Item.Genres.Any())
                            result.Item.Genres = PhoenixAdultGenres.Cleanup(result.Item.Genres, result.Item.Name);
                    }
                }
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url
        });

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

            string clearName = Lang.TextInfo.ToTitleCase(title),
                   clearSite = siteName;

            clearName = clearName.Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

            clearName = Regex.Replace(clearName, @"[^a-zA-Z0-9 ]", " ");
            clearSite = Regex.Replace(clearSite, @"\W", string.Empty);

            bool matched = false;
            while (clearName.Contains(" ", StringComparison.OrdinalIgnoreCase))
            {
                clearName = clearName.Replace(" ", string.Empty, 1, StringComparison.OrdinalIgnoreCase);
                if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                clearName = clearName.Replace(clearSite, string.Empty, StringComparison.OrdinalIgnoreCase);
                clearName = string.Join(" ", clearName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return clearName;
        }

        public static (string, DateTime?) GetDateFromTitle(string title)
        {
            string searchDate,
                   searchTitle = title;
            var regExRules = new Dictionary<string, string> {
                { @"\b\d{4} \d{2} \d{2}\b", "yyyy MM dd" },
                { @"\b\d{2} \d{2} \d{2}\b", "yy MM dd" }
            };
            (string, DateTime?) searchData = (searchTitle, null);

            foreach (var regExRule in regExRules)
            {
                var regEx = Regex.Match(searchTitle, regExRule.Key);
                if (regEx.Groups.Count > 0)
                    if (DateTime.TryParseExact(regEx.Groups[0].Value, regExRule.Value, Lang, DateTimeStyles.None, out DateTime searchDateObj))
                    {
                        searchDate = searchDateObj.ToString("yyyy-MM-dd", Lang);
                        searchTitle = Regex.Replace(searchTitle, regExRule.Key, string.Empty).Trim();

                        searchData = (searchTitle, searchDateObj);
                        break;
                    }
            }

            return searchData;
        }

        public static string ReplaceAbbrieviation(string title)
        {
            string newTitle = title;

            foreach (var abbrieviation in PhoenixAdultList.AbbrieviationList)
            {
                Regex regex = new Regex(abbrieviation.Key, RegexOptions.IgnoreCase);
                if (regex.IsMatch(title))
                {
                    newTitle = regex.Replace(title, abbrieviation.Value, 1);
                    break;
                }
            }

            return newTitle;
        }
    }
}
