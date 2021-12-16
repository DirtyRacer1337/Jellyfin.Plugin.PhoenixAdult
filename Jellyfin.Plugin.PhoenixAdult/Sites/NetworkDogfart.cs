using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class NetworkDogfart : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var url = Helper.GetSearchBaseURL(siteNum) + $"/tour/sites/{Helper.GetSearchSiteName(siteNum).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)}/{searchTitle.Replace(" ", "_", StringComparison.OrdinalIgnoreCase)}/";
            var sceneID = new List<string> { Helper.Encode(new Uri(url).AbsolutePath) };
            var directResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID.ToArray(), searchDate, cancellationToken).ConfigureAwait(false);
            if (directResult.Any())
            {
                result.AddRange(directResult);
                return result;
            }

            url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//a[contains(@class, 'thumbnail')]");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.Attributes["href"].Value.Split('?')[0]);
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText(".//div/h3[@class='scene-title']"),
                    posterURL = $"https:{searchResult.SelectSingleText(".//img/@src")}",
                    subSite = searchResult.SelectSingleText(".//div/p[@class='help-block']").Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

                var res = new RemoteSearchResult
                {
                    Name = $"{sceneName} from {subSite}",
                    ImageUrl = posterURL,
                };

                if (searchDate.HasValue)
                {
                    curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

                    res.PremiereDate = searchDate.Value;
                }

                res.ProviderIds.Add(Plugin.Instance.Name, curID);

                if (subSite == Helper.GetSearchSiteName(siteNum))
                {
                    res.IndexNumber = 100 - LevenshteinDistance.Calculate(searchTitle, sceneName, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    res.IndexNumber = 60 - LevenshteinDistance.Calculate(searchTitle, sceneName, StringComparison.OrdinalIgnoreCase);
                }

                result.Add(res);
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            string sceneURL = Helper.Decode(sceneID[0]),
                sceneDate = string.Empty;

            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            if (sceneID.Length > 1)
            {
                sceneDate = sceneID[1];
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//div[@class='icon-container']/a/@title");
            result.Item.Overview = sceneData.SelectSingleText("//div[contains(@class, 'description')]").Replace("...read more", string.Empty, StringComparison.OrdinalIgnoreCase);
            result.Item.AddStudio("Dogfart Network");
            var studio = sceneData.SelectSingleText("//h3[@class='site-name']").Replace(".com", string.Empty, 1, StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(studio))
            {
                result.Item.AddStudio(studio);
            }

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodesSafe("//div[@class='categories']/p/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//h4[@class='more-scenes']/a");
            foreach (var actorLink in actorsNode)
            {
                var actorName = actorLink.InnerText;

                result.People.Add(new PersonInfo
                {
                    Name = actorName,
                });
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var poster = sceneData.SelectSingleText("//div[@class='icon-container']//img/@src");
            if (!string.IsNullOrEmpty(poster))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = "https:" + poster,
                    Type = ImageType.Primary,
                });
            }

            var img = sceneData.SelectNodesSafe("//div[contains(@class, 'preview-image-container')]//a");
            foreach (var sceneImages in img)
            {
                var url = Helper.GetSearchBaseURL(siteNum) + sceneImages.Attributes["href"].Value;
                var posterHTML = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var posterData = posterHTML.SelectSingleText("//div[contains(@class, 'remove-bs-padding')]/img/@src");
                if (!string.IsNullOrEmpty(posterData))
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = posterData,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            return result;
        }
    }
}
