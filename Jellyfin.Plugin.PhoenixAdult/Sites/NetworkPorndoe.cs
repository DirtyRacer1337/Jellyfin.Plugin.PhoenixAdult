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

            var searchResults = data.SelectNodesSafe("//div[contains(@class, 'main-content-videos')]//div[contains(@class, 'card-video')]");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = searchResult.SelectSingleText(".//a/@href"),
                        curID = Helper.Encode(sceneURL),
                        sceneName = searchResult.SelectSingleText(".//a/@aria-label"),
                        sceneDate = searchResult.SelectSingleText(".//p[contains(@class, 'extra-info') and not(contains(@class, 'actors'))]"),
                        scenePoster = searchResult.SelectSingleText(".//div[contains(@class, 'thumb')]/@data-bg");

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

            result.Item.Name = sceneData.SelectSingleText("//h1[@class='no-space transform-none']");
            result.Item.Overview = sceneData.SelectSingleText("//meta[@name='description']/@content");
            result.Item.AddStudio("Porndoe Premium");

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

            var actorsNode = sceneData.SelectNodesSafe("//span[@class='group inline']/a");
            foreach (var actorLink in actorsNode)
            {
                var actorPageURL = actorLink.Attributes["href"].Value;
                var actorDate = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorName = actorDate.SelectSingleText("//div[@data-item='c-13 r-11 m-c-15 / middle']/h1");
                var res = new PersonInfo
                {
                    Name = actorName,
                };

                var actorPhoto = actorDate.SelectSingleText("//div[@class='avatar']/picture[2]/img/@data-src");
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
