using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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

            var searchResults = data.SelectNodes("//div[contains(@class, 'main-content-videos')]//div[contains(@class, 'card-video')]");
            if (searchResults != null)
            {
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleText(".//div[@data-item='c-11 r-11 / bottom']/a/@href"),
                            curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleText(".//div[@data-item='c-11 r-11 / bottom']/a/@title"),
                            sceneDate = searchResult.SelectSingleText(".//div[@data-item='c-21 r-21 / middle right']/p"),
                            scenePoster = searchResult.SelectSingleText(".//div[contains(@class, 'thumb')]/@data-bg");

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster,
                    };

                    if (DateTime.TryParseExact(sceneDate, "dd MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                    }

                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(string[] sceneID, CancellationToken cancellationToken)
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

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            var sceneURL = Helper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleText("//h1[@class='no-space transform-none']");
            var description = HttpUtility.HtmlDecode(sceneData.SelectSingleText("//meta[@name='description']/@content"));
            result.Item.Overview = description;
            result.Item.AddStudio("Porndoe Premium");

            var dateNode = sceneData.SelectSingleNode("//div[@class='h5 h5-published nowrap color-rgba255-06']");
            if (dateNode != null)
            {
                if (DateTime.TryParseExact(dateNode.InnerText.Trim(), "dd MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genres = sceneData.SelectNodes("//a[@class='inline-links']");
            foreach (var genreLink in genres)
            {
                var genreName = genreLink.InnerText.Trim();

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodes("//span[@class='group inline']/a");
            if (actorsNode != null)
            {
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
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (item == null)
            {
                return result;
            }

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
            {
                return result;
            }

            var sceneID = externalId.Split('#');

            var sceneURL = Helper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var xpaths = new List<string>
            {
                "//picture[@class='poster']//img",
                "//div[@id='gallery-thumbs']//img",
            };

            foreach (var xpath in xpaths)
            {
                var poster = sceneData.SelectSingleNode(xpath);
                if (poster != null)
                {
                    var img = poster.Attributes["src"].Value;

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
