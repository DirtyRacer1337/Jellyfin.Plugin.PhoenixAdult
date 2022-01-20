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
    public class NetworkNubiles : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            if (searchDate.HasValue)
            {
                var date = searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var url = Helper.GetSearchSearchURL(siteNum) + $"date/{date}/{date}";
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodesSafe("//div[contains(@class, 'content-grid-item')]");
                foreach (var searchResult in searchResults)
                {
                    var sceneNum = searchResult.SelectSingleText(".//span[@class='title']/a/@href").Split('/')[3];
                    var sceneURL = new Uri(Helper.GetSearchSearchURL(siteNum) + $"watch/{sceneNum}");
                    string curID = Helper.Encode(sceneURL.AbsolutePath),
                        sceneName = searchResult.SelectSingleText(".//span[@class='title']/a | //h2"),
                        posterURL = searchResult.SelectSingleText(".//picture/img/@data-srcset").Split(",")[0].Split(" ")[0],
                        sceneDate = searchResult.SelectSingleText(".//span[@class='date']");

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = posterURL,
                    };

                    if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                    }

                    result.Add(res);
                }
            }
            else
            {
                if (int.TryParse(searchTitle.Split()[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sceneNum))
                {
                    var url = Helper.GetSearchSearchURL(siteNum) + $"watch/{sceneNum}";
                    var sceneURL = new Uri(url);
                    var sceneID = new string[] { Helper.Encode(sceneURL.AbsolutePath) };

                    var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID.ToArray(), searchDate, cancellationToken).ConfigureAwait(false);
                    if (searchResult.Any())
                    {
                        result.AddRange(searchResult);
                    }
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

            result.Item.Name = sceneData.SelectSingleText("//div[contains(@class, 'content-pane-title')]//h2");
            var description = sceneData.SelectSingleText("//div[@class='col-12 content-pane-column']/div");
            if (string.IsNullOrEmpty(description))
            {
                var paragraphs = sceneData.SelectNodesSafe("//div[@class='col-12 content-pane-column']//p");
                foreach (var paragraph in paragraphs)
                {
                    description += "\n\n" + paragraph.InnerText.Trim();
                }
            }

            if (!string.IsNullOrEmpty(description))
            {
                result.Item.Overview = description;
            }

            result.Item.AddStudio("Nubiles");

            var sceneDate = sceneData.SelectSingleText("//div[contains(@class, 'content-pane')]//span[@class='date']");
            if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodesSafe("//div[@class='categories']/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[contains(@class, 'content-pane-performer')]/a");
            foreach (var actorLink in actorsNode)
            {
                string actorName = actorLink.InnerText,
                    actorPageURL = Helper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value;

                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);
                var actorPhotoURL = "http:" + actorPage.SelectSingleText("//div[contains(@class, 'model-profile')]//img/@src");

                result.People.Add(new PersonInfo
                {
                    Name = actorName,
                    ImageUrl = actorPhotoURL,
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

            var poster = sceneData.SelectSingleText("//video/@poster");
            if (!string.IsNullOrEmpty(poster))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = poster,
                    Type = ImageType.Primary,
                });
            }

            var photoPageURL = "https://nubiles-porn.com/photo/gallery/" + sceneID[0];
            var photoPage = await HTML.ElementFromURL(photoPageURL, cancellationToken).ConfigureAwait(false);
            var sceneImages = photoPage.SelectNodesSafe("//div[@class='img-wrapper']//source[1]");
            foreach (var sceneImage in sceneImages)
            {
                var posterURL = sceneImage.Attributes["src"].Value;

                result.Add(new RemoteImageInfo
                {
                    Url = posterURL,
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
