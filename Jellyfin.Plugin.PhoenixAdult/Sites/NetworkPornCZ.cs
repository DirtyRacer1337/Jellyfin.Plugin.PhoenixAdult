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
    public class NetworkPornCZ : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
            {
                return result;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[contains(@class, 'product-items')]//div[contains(@class, 'video-box-item')]");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//a/@href"));
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText(".//div[@class='product-item-bottom']//a"),
                    scenePoster = Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//img/@data-src");

                var res = new RemoteSearchResult
                {
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (searchDate.HasValue)
                {
                    curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
                    res.PremiereDate = searchDate.Value;
                }

                res.ProviderIds.Add(Plugin.Instance.Name, curID);

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

            result.Item.Name = sceneData.SelectSingleText("//h1");
            var descriptionNodes = sceneData.SelectNodesSafe("//div[@class='heading-detail']//p");
            foreach (var description in descriptionNodes)
            {
                result.Item.Overview = description.InnerText.Trim() + "\n";
            }

            result.Item.AddStudio("PornCZ");

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genres = sceneData.SelectNodesSafe("//div[contains(@class, 'video-info-item')]//a[contains(@href, 'category')]");
            foreach (var genreLink in genres)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actors = sceneData.SelectNodesSafe("//div[contains(@class, 'video-info-item')]//a[not(contains(@href, 'category'))]");
            foreach (var actorLink in actors)
            {
                string actorName = actorLink.InnerText,
                        actorPageURL = Helper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                        actorPhoto = string.Empty;

                var res = new PersonInfo
                {
                    Name = actorName,
                };

                var actorHTML = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);
                var img = actorHTML.SelectSingleText("//div[@class='model-heading-photo']//img/@src");
                if (!string.IsNullOrEmpty(img))
                {
                    if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        img = Helper.GetSearchBaseURL(siteNum) + img;
                    }

                    actorPhoto = img;
                }

                if (!string.IsNullOrEmpty(actorPhoto))
                {
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

            var img = sceneData.SelectSingleText("//div[@id='video-poster']/@data-poster");
            if (!string.IsNullOrEmpty(img))
            {
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = Helper.GetSearchBaseURL(siteNum) + img;
                }

                result.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Primary,
                });
            }

            var sceneImages = sceneData.SelectNodesSafe("//div[@id='gallery']//img");
            foreach (var sceneImage in sceneImages)
            {
                img = sceneImage.Attributes["data-src"].Value;
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = Helper.GetSearchBaseURL(siteNum) + img;
                }

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

            return result;
        }
    }
}
