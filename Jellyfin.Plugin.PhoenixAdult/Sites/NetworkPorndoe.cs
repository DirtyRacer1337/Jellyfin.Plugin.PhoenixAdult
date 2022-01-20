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
    public class NetworkPorndoe : IProviderBase
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

            var searchResults = data.SelectNodesSafe("//div[@class='global-video-listing']//div[@class='global-video-card']");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(searchResult.SelectSingleText(".//a[contains(@class, '-g-vc-title-url')]/@href"));
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText(".//a[contains(@class, '-g-vc-title-url')]/@title"),
                    sceneDate = searchResult.SelectSingleText(".//div[@class='-g-vc-item-date']"),
                    scenePoster = searchResult.SelectSingleText(".//div[contains(@class, '-g-vc-thumb')]/@data-bg");

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (DateTime.TryParseExact(sceneDate, "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
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

            result.Item.Name = sceneData.SelectSingleText("//h1[@class='no-space transform-none']");
            result.Item.Overview = sceneData.SelectSingleText("//meta[@name='description']/@content");
            result.Item.AddStudio("Porndoe Premium");
            var studio = sceneData.SelectSingleText("//div[@class='actors']/h2/a");
            if (!string.IsNullOrEmpty(studio))
            {
                result.Item.AddStudio(studio);
            }

            var dateNode = sceneData.SelectSingleText("//div[@class='h5 h5-published nowrap color-rgba255-06']");
            if (DateTime.TryParseExact(dateNode.Split("•").Last().Trim(), "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genres = sceneData.SelectNodesSafe("//a[@class='inline-links']");
            foreach (var genreLink in genres)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[@class='actors']//a[contains(@href, '/models/')]");
            foreach (var actorLink in actorsNode)
            {
                var actorPageURL = actorLink.Attributes["href"].Value;
                var actorDate = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorName = actorLink.SelectSingleText(".//strong");
                var res = new PersonInfo
                {
                    Name = actorName,
                };

                var actorPhoto = actorDate.SelectSingleText("//div[@class='-api-poster-item']//img/@data-src");
                if (!string.IsNullOrEmpty(actorPhoto))
                {
                    if (!actorPhoto.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        actorPhoto = Helper.GetSearchBaseURL(siteNum) + actorPhoto;
                    }

                    res.ImageUrl = actorPhoto;
                }

                result.People.Add(res);
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

            var xpaths = new List<string>
            {
                "//picture[@class='poster']//img/@src",
                "//div[@id='gallery-thumbs']//img/@src",
            };

            foreach (var xpath in xpaths)
            {
                var img = sceneData.SelectSingleText(xpath);
                if (!string.IsNullOrEmpty(img))
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = img,
                        Type = ImageType.Primary,
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = img,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            return result;
        }
    }
}
