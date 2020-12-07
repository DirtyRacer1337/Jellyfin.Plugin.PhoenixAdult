using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
#else
using System.Net.Http;
using Microsoft.Extensions.Logging;
#endif

namespace PhoenixAdult
{
    public class Provider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
#if __EMBY__
        public Provider(ILogManager logger, IHttpClient http)
        {
            if (logger != null)
            {
                Log = logger.GetLogger(this.Name);
            }

            Http = http;
        }

        public static IHttpClient Http { get; set; }
#else
        public Provider(ILogger<Provider> logger, IHttpClientFactory http)
        {
            Log = logger;
            Http = http;
        }

        public static IHttpClientFactory Http { get; set; }
#endif

        public static ILogger Log { get; set; }

        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            List<RemoteSearchResult> result = new List<RemoteSearchResult>();

            if (searchInfo == null)
            {
                return result;
            }

            Logger.Info($"searchInfo.Name: {searchInfo.Name}");

            var title = Helper.ReplaceAbbrieviation(searchInfo.Name);
            var site = Helper.GetSiteFromTitle(title);
            if (site.Key == null)
            {
                string newTitle;
                if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.DefaultSiteName))
                {
                    newTitle = $"{Plugin.Instance.Configuration.DefaultSiteName} {searchInfo.Name}";
                }
                else
                {
                    newTitle = Helper.GetSiteNameFromTitle(searchInfo.Name);
                }

                if (!string.IsNullOrEmpty(newTitle) && !newTitle.Equals(searchInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info($"newTitle: {newTitle}");

                    title = Helper.ReplaceAbbrieviation(newTitle);
                    site = Helper.GetSiteFromTitle(title);
                }

                if (site.Key == null)
                {
                    return result;
                }
            }

            string searchTitle = Helper.GetClearTitle(title, site.Value),
                   searchDate = string.Empty;
            DateTime? searchDateObj;
            var titleAfterDate = Helper.GetDateFromTitle(searchTitle);

            var siteNum = new int[2]
            {
                site.Key[0],
                site.Key[1],
            };
            searchTitle = titleAfterDate.Item1;
            searchDateObj = titleAfterDate.Item2;
            if (searchDateObj.HasValue)
            {
                searchDate = searchDateObj.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else
            {
                if (searchInfo.PremiereDate.HasValue)
                {
#if __EMBY__
                    searchDateObj = searchInfo.PremiereDate.Value.DateTime;
#else
                    searchDateObj = searchInfo.PremiereDate.Value;
#endif
                    searchDate = searchInfo.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }

            if (string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            Logger.Info($"site: {siteNum[0]}:{siteNum[1]} ({site.Value})");
            Logger.Info($"searchTitle: {searchTitle}");
            Logger.Info($"searchDate: {searchDate}");

            var provider = Helper.GetProviderBySiteID(siteNum[0]);
            if (provider != null)
            {
                Logger.Info($"provider: {provider}");

                try
                {
                    result = await provider.Search(siteNum, searchTitle, searchDateObj, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Info($"Search error: \"{e.Message}\"");
                    Logger.Error(e.ToString());
                }

                if (result.Any())
                {
                    foreach (var scene in result)
                    {
                        scene.Name = scene.Name.Trim();
                    }

                    if (result.Any(scene => scene.IndexNumber.HasValue))
                    {
                        result = result.OrderByDescending(scene => scene.IndexNumber.HasValue).ThenByDescending(scene => scene.IndexNumber).ToList();
                    }
                    else if (!string.IsNullOrEmpty(searchDate) && result.All(scene => scene.PremiereDate.HasValue) && result.Any(scene => scene.PremiereDate.Value != searchDateObj))
                    {
                        result = result.OrderBy(scene => Math.Abs((searchDateObj - scene.PremiereDate).Value.TotalDays)).ToList();
                    }
                    else
                    {
                        result = result.OrderByDescending(scene => 100 - LevenshteinDistance.Calculate(searchTitle, scene.Name, StringComparison.OrdinalIgnoreCase)).ToList();
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
                Item = new Movie(),
            };

            if (info == null)
            {
                return result;
            }

            var sceneID = info.ProviderIds;
            if (!sceneID.ContainsKey(this.Name))
            {
                var searchResults = await this.GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    sceneID = searchResults.First().ProviderIds;
                }
            }

            if (!sceneID.TryGetValue(this.Name, out string externalID))
            {
                return result;
            }

            var curID = externalID.Split('#');
            if (curID.Length < 3)
            {
                return result;
            }

            int[] siteNum = new int[2] { int.Parse(curID[0], CultureInfo.InvariantCulture), int.Parse(curID[1], CultureInfo.InvariantCulture) };

            var provider = Helper.GetProviderBySiteID(siteNum[0]);
            if (provider != null)
            {
                Logger.Info($"PhoenixAdult ID: {externalID}");

                try
                {
                    result = await provider.Update(siteNum, curID.Skip(2).ToArray(), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Info($"Update error: \"{e.Message}\"");
                    Logger.Error(e.ToString());
                }

                if (!string.IsNullOrEmpty(result.Item.Name))
                {
                    result.HasMetadata = true;
                    result.Item.OfficialRating = "XXX";
                    result.Item.ProviderIds.Update(this.Name, sceneID[this.Name]);

                    result.Item.Name = HttpUtility.HtmlDecode(result.Item.Name).Trim();

                    if (!string.IsNullOrEmpty(result.Item.Overview))
                    {
                        result.Item.Overview = HttpUtility.HtmlDecode(result.Item.Overview).Trim();
                    }

                    var newStudios = new List<string>();
                    foreach (var studio in result.Item.Studios)
                    {
                        var studioName = studio.Trim();
                        studioName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(studioName);

                        if (!newStudios.Contains(studioName))
                        {
                            newStudios.Add(studioName);
                        }
                    }

                    result.Item.Studios = newStudios.ToArray();

                    if (result.Item.PremiereDate.HasValue)
                    {
                        result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                    }

                    if (result.People != null && result.People.Any())
                    {
                        result.People = Actors.Cleanup(result);
                    }

                    if (result.Item.Genres != null && result.Item.Genres.Any())
                    {
                        result.Item.Genres = Genres.Cleanup(result.Item.Genres, result.Item.Name, result.People);
                    }

                    if (!string.IsNullOrEmpty(result.Item.ExternalId))
                    {
                        result.Item.ProviderIds.Update(this.Name + "URL", result.Item.ExternalId);
                    }
                }
            }

            return result;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                EnableDefaultUserAgent = false,
                UserAgent = HTTP.GetUserAgent(),
            });
        }
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", HTTP.GetUserAgent());

            return Http.CreateClient().SendAsync(request, cancellationToken);
        }
#endif
    }
}
