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

            if (!data.SelectSingleText("//title").Contains("Search for", StringComparison.OrdinalIgnoreCase))
            {
                var sceneURL = new Uri(data.SelectSingleText("//div[@class='user--guest']//a/@href"));
                var sceneID = new string[] { Helper.Encode(sceneURL.AbsolutePath) };

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, searchDate, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);
                }
            }
            else
            {
                var searchResults = data.SelectNodesSafe("//div[@class='thumbnails']/div");
                foreach (var searchResult in searchResults)
                {
                    var sceneURL = new Uri(searchResult.SelectSingleText(".//a/@href"));

                    string curID = Helper.Encode(sceneURL.AbsolutePath),
                        sceneName = searchResult.SelectSingleText(".//div[contains(@class, 'thumbnail-title')]//a"),
                        sceneDate = searchResult.SelectSingleText("./@release");

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                    };

                    if (DateTime.TryParseExact(sceneDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                    }

                    var scenePoster = searchResult.SelectSingleText(".//div[@class='thumbnail-image']/a/@style");
                    if (!string.IsNullOrEmpty(scenePoster))
                    {
                        scenePoster = scenePoster.Split('(')[1].Split(')')[0];
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

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//h1[@class='watchpage-title']");
            result.Item.AddStudio("LegalPorno");
            var studio = sceneData.SelectSingleText("//a[@class='watchpage-studioname']/text()");
            if (!string.IsNullOrEmpty(studio))
            {
                result.Item.AddStudio(studio);
            }

            var sceneDate = sceneData.SelectSingleText("//span[@class='scene-description__detail']//a");
            if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodesSafe("//dd/a[contains(@href, '/niche/')]");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//dd/a[contains(@href, 'model') and not(contains(@href, 'forum'))]");
            foreach (var actorLink in actorsNode)
            {
                var actor = new PersonInfo
                {
                    Name = actorLink.InnerText,
                };

                var actorPage = await HTML.ElementFromURL(actorLink.Attributes["href"].Value, cancellationToken).ConfigureAwait(false);
                var actorPhoto = actorPage.SelectSingleText("//div[@class='model--avatar']//img/@src");
                if (!string.IsNullOrEmpty(actorPhoto))
                {
                    actor.ImageUrl = actorPhoto;
                }

                result.People.Add(actor);
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

            var scenePoster = sceneData.SelectSingleText("//div[@id='player']/@style").Split('(')[1].Split(')')[0];
            result.Add(new RemoteImageInfo
            {
                Url = scenePoster,
                Type = ImageType.Primary,
            });

            var scenePosters = sceneData.SelectNodesSafe("//div[contains(@class, 'thumbs2 gallery')]//img");
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

            return result;
        }
    }
}
