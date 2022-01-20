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
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//a[contains(@href, '/video')]/@href"));
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText(".//span[@class='thmb_ttl']"),
                    scenePoster = $"https:{searchResult.SelectSingleText(".//img/@data-src")}",
                    sceneDate = searchResult.SelectSingleText(".//span[contains(@class, 'thmb_mr_2')]");

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

            result.Item.Name = sceneData.SelectSingleText("//h1");
            result.Item.Overview = sceneData.SelectSingleText("//div[@class='vdoDesc']");
            result.Item.AddStudio("Bang Bros");
            var studio = sceneData.SelectSingleText("//a[contains(@href, '/websites') and not(@class)]");
            if (!string.IsNullOrEmpty(studio))
            {
                result.Item.AddStudio(studio);
            }

            var dateNode = sceneData.SelectSingleText("//span[contains(@class, 'thmb_mr_2')]");
            if (DateTime.TryParseExact(dateNode, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
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
                actorPhoto = $"https:{actorHTML.SelectSingleText("//div[@class='profilePic_in']//img/@src")}";

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
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

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
