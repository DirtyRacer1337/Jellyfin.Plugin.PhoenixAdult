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
    public class SiteBangBros : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            searchTitle = searchTitle.Replace(" ", "+", StringComparison.OrdinalIgnoreCase);
            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[contains(@class, 'elipsTxt')]//div[@class='echThumb']");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a[contains(@href, '/video')]").Attributes["href"].Value,
                        curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                        sceneName = searchResult.SelectSingleNode(".//span[@class='thmb_ttl']").InnerText,
                        scenePoster = $"https:{searchResult.SelectSingleNode(".//img").Attributes["data-src"].Value}",
                        sceneDate = searchResult.SelectSingleNode(".//span[contains(@class, 'thmb_mr_2')]").InnerText.Trim();

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    res.PremiereDate = sceneDateObj;
                }

                result.Add(res);
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

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText;
            result.Item.Overview = sceneData.SelectSingleNode("//div[@class='vdoDesc']").InnerText;
            result.Item.AddStudio("Bang Bros");

            var dateNode = sceneData.SelectSingleNode("//span[contains(@class, 'thmb_mr_2')]");
            if (dateNode != null)
            {
                if (DateTime.TryParseExact(dateNode.InnerText.Trim(), "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodesSafe("//div[contains(@class, 'vdoTags')]//a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[@class='vdoCast']//a[contains(@href, '/model')]");
            foreach (var actorLink in actorsNode)
            {
                string actorName = actorLink.InnerText,
                        actorPageURL = Helper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                        actorPhoto;

                var actorHTML = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);
                actorPhoto = $"https:{actorHTML.SelectSingleNode("//div[@class='profilePic_in']//img").Attributes["src"].Value}";

                result.People.Add(new PersonInfo
                {
                    Name = actorName,
                    ImageUrl = actorPhoto,
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
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var imgNode = sceneData.SelectNodesSafe("//img[contains(@id, 'player-overlay-image')]");
            foreach (var sceneImages in imgNode)
            {
                result.Add(new RemoteImageInfo
                {
                    Url = $"https:{sceneImages.Attributes["src"].Value}",
                    Type = ImageType.Primary,
                });
            }

            imgNode = sceneData.SelectNodesSafe("//div[@id='img-slider']//img");
            foreach (var sceneImages in imgNode)
            {
                result.Add(new RemoteImageInfo
                {
                    Url = $"https:{sceneImages.Attributes["src"].Value}",
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
