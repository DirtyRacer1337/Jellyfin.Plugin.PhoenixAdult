using System;
using System.Collections.Generic;
using System.Globalization;
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

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//a[contains(@class, 'thumbnail')]");
            if (searchResults != null)
            {
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = Helper.GetSearchBaseURL(siteNum) + searchResult.Attributes["href"].Value.Split('?')[0],
                            curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div/h3[@class='scene-title']").InnerText,
                            posterURL = $"https:{searchResult.SelectSingleNode(".//img").Attributes["src"].Value}",
                            subSite = searchResult.SelectSingleNode(".//div/p[@class='help-block']").InnerText.Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

                    var res = new RemoteSearchResult
                    {
                        Name = $"{sceneName} from {subSite}",
                        ImageUrl = posterURL,
                    };

                    if (searchDate.HasValue)
                    {
                        curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
                    }

                    res.ProviderIds.Add(Plugin.Instance.Name, curID);

                    if (subSite == Helper.GetSearchSiteName(siteNum))
                    {
                        res.IndexNumber = 100 - LevenshteinDistance.Calculate(searchTitle, sceneName);
                    }
                    else
                    {
                        res.IndexNumber = 60 - LevenshteinDistance.Calculate(searchTitle, sceneName);
                    }

                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
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

            if (sceneID.Length > 1)
            {
                sceneDate = sceneID[1];
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//div[@class='icon-container']/a").Attributes["title"].Value;
            result.Item.Overview = sceneData.SelectSingleNode("//div[contains(@class, 'description')]").InnerText.Replace("...read more", string.Empty, StringComparison.OrdinalIgnoreCase);
            result.Item.AddStudio("Dogfart Network");

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodes("//div[@class='categories']/p/a");
            if (genreNode != null)
            {
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Item.AddGenre(genreName);
                }
            }

            var actorsNode = sceneData.SelectNodes("//h4[@class='more-scenes']/a");
            if (actorsNode != null)
            {
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText;

                    result.People.Add(new PersonInfo
                    {
                        Name = actorName,
                    });
                }
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

            string sceneURL = Helper.Decode(sceneID[0]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var poster = sceneData.SelectSingleNode("//div[@class='icon-container']//img");
            if (poster != null)
            {
                result.Add(new RemoteImageInfo
                {
                    Url = $"https:{poster.Attributes["src"].Value}",
                    Type = ImageType.Primary,
                });
            }

            var img = sceneData.SelectNodes("//div[contains(@class, 'preview-image-container')]//a");
            if (img != null)
            {
                foreach (var sceneImages in img)
                {
                    var url = Helper.GetSearchBaseURL(siteNum) + sceneImages.Attributes["href"].Value;
                    var posterHTML = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                    var posterData = posterHTML.SelectSingleNode("//div[contains(@class, 'remove-bs-padding')]/img").Attributes["src"].Value;
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
