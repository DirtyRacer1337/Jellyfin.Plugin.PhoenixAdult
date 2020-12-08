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
    public class SiteLegalPorno : IProviderBase
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

            if (!data.SelectSingleNode("//title").InnerText.Contains("Search for", StringComparison.OrdinalIgnoreCase))
            {
                string sceneURL = data.SelectSingleNode("//div[@class='user--guest']//a").Attributes["href"].Value,
                       curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}";
                var sceneID = curID.Split('#').Skip(2).ToArray();

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);
                }
            }
            else
            {
                var searchResults = data.SelectNodes("//div[@class='thumbnails']/div");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div[contains(@class, 'thumbnail-title')]//a").InnerText,
                            scenePoster = string.Empty,
                            sceneDate = searchResult.SelectSingleNode(".").Attributes["release"].Value;

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                    };

                    if (DateTime.TryParseExact(sceneDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                    }

                    var scenePosterNode = searchResult.SelectSingleNode(".//div[@class='thumbnail-image']/a");
                    if (scenePosterNode != null && scenePosterNode.Attributes.Contains("style"))
                    {
                        scenePoster = scenePosterNode.Attributes["style"].Value.Split('(')[1].Split(')')[0];
                    }

                    if (!string.IsNullOrEmpty(scenePoster))
                    {
                        res.ImageUrl = scenePoster;
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

            var sceneURL = Helper.Decode(sceneID[0]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleNode("//h1[@class='watchpage-title']").InnerText;
            result.Item.AddStudio("LegalPorno");

            var sceneDate = sceneData.SelectSingleNode("//span[@class='scene-description__detail']//a").InnerText.Trim();
            if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodes("//dd/a[contains(@href, '/niche/')]");
            if (genreNode != null)
            {
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Item.AddGenre(genreName);
                }
            }

            var actorsNode = sceneData.SelectNodes("//dd/a[contains(@href, 'model') and not(contains(@href, 'forum'))]");
            if (actorsNode != null)
            {
                foreach (var actorLink in actorsNode)
                {
                    var actor = new PersonInfo
                    {
                        Name = actorLink.InnerText,
                    };

                    var actorPage = await HTML.ElementFromURL(actorLink.Attributes["href"].Value, cancellationToken).ConfigureAwait(false);
                    var actorPhotoNode = actorPage.SelectSingleNode("//div[@class='model--avatar']//img");
                    if (actorPhotoNode != null)
                    {
                        actor.ImageUrl = actorPhotoNode.Attributes["src"].Value;
                    }

                    result.People.Add(actor);
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

            var sceneURL = Helper.Decode(sceneID[0]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var scenePoster = sceneData.SelectSingleNode("//div[@id='player']").Attributes["style"].Value.Split('(')[1].Split(')')[0];
            result.Add(new RemoteImageInfo
            {
                Url = scenePoster,
                Type = ImageType.Primary,
            });

            var scenePosters = sceneData.SelectNodes("//div[contains(@class, 'thumbs2 gallery')]//img");
            if (scenePosters != null)
            {
                foreach (var poster in scenePosters)
                {
                    scenePoster = poster.Attributes["src"].Value.Split('?')[0];
                    result.Add(new RemoteImageInfo
                    {
                        Url = scenePoster,
                        Type = ImageType.Primary,
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = scenePoster,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            return result;
        }
    }
}
