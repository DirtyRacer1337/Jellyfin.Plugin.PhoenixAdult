using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

#if __EMBY__
using MediaBrowser.Model.Logging;
#else
using Microsoft.Extensions.Logging;
#endif

namespace PhoenixAdult
{
    public class Provider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public Provider(
#if __EMBY__
            ILogManager logger,
#else
            ILogger<Provider> logger,
#endif
            IHttpClient http)
        {
#if __EMBY__
            if (logger != null)
            {
                Log = logger.GetLogger(this.Name);
            }
#else
            Log = logger;
#endif
            Http = http;
        }

        public static ILogger Log { get; set; }

        public static IHttpClient Http { get; set; }

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
            if (site.Key != null)
            {
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
                        searchDate = searchInfo.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                }

                Logger.Info($"site: {siteNum[0]}:{siteNum[1]} ({site.Value})");
                Logger.Info($"searchTitle: {searchTitle}");
                Logger.Info($"searchDate: {searchDate}");

                var provider = Helper.GetProviderBySiteID(siteNum[0]);
                if (provider != null)
                {
                    Logger.Info($"provider: {provider}");
                    result = await provider.Search(siteNum, searchTitle, searchDateObj, cancellationToken).ConfigureAwait(false);
                    if (result.Any())
                    {
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
                            result = result.OrderByDescending(scene => 100 - LevenshteinDistance.Calculate(searchTitle, scene.Name)).ToList();
                        }
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

            var provider = Helper.GetProviderBySiteID(int.Parse(curID[0], CultureInfo.InvariantCulture));
            if (provider != null)
            {
                Logger.Info($"PhoenixAdult ID: {externalID}");
                result = await provider.Update(curID, cancellationToken).ConfigureAwait(false);
                if (result.Item != new Movie())
                {
                    result.HasMetadata = true;
                    result.Item.OfficialRating = "XXX";
                    result.Item.ProviderIds = sceneID;

                    if (result.Item.PremiereDate.HasValue)
                    {
                        result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                    }

                    if (result.People != null && result.People.Any() && !Plugin.Instance.Configuration.DisableActors)
                    {
                        result.People = Actors.Cleanup(result);
                    }

                    if (result.Item.Genres != null && result.Item.Genres.Any())
                    {
                        result.Item.Genres = Genres.Cleanup(result.Item.Genres, result.Item.Name, result.People);
                    }
                }
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
            => Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                EnableDefaultUserAgent = false,
                UserAgent = HTTP.GetUserAgent(),
            });
    }
}
