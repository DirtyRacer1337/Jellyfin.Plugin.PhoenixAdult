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
    public class NetworkAdultEmpire : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
            {
                return result;
            }

            var searchTitleSplit = searchTitle.Split(' ');
            if (int.TryParse(searchTitleSplit.First(), out var id) && id > 100)
            {
                searchTitle = string.Join(" ", searchTitleSplit.Skip(1));
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + $"/{searchTitleSplit.First()}");
                var sceneID = new List<string> { Helper.Encode(sceneURL.AbsolutePath) };

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID.ToArray(), searchDate, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);

                    return result;
                }
            }

            if (string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[@class='product-card']");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//a/@href"));
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText(".//div[@class='item-title']/a/@title"),
                    posterURL = searchResult.SelectSingleText(".//img/@data-src");

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = posterURL,
                };

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

            string sceneURL = Helper.Decode(sceneID[0]);

            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//h1/text()");
            result.Item.Overview = sceneData.SelectSingleText("//h4[contains(@class, 'synopsis')]");

            var studioName = sceneData.SelectSingleText("//a[@label='Studio']");
            if (!string.IsNullOrEmpty(studioName))
            {
                result.Item.AddStudio(studioName);
            }

            var sceneDate = sceneData.SelectSingleText("//small[contains(text(), 'Released')]/following-sibling::text()");
            if (DateTime.TryParseExact(sceneDate, "MMM dd yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodesSafe("//div[h2[contains(., 'Categories')]]//a[@label='Category']");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//a[@label='Performer']");
            foreach (var actorLink in actorsNode)
            {
                string actorName = actorLink.InnerText,
                    actorPhoto = actorLink.SelectSingleText(".//img/@src");

                var res = new PersonInfo
                {
                    Name = actorName,
                };

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

            var poster = sceneData.SelectSingleText("//a[@id='front-cover']/@data-href");
            if (!string.IsNullOrEmpty(poster))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = poster,
                    Type = ImageType.Primary,
                });
            }

            var sceneImages = sceneData.SelectNodesSafe("//a[@rel='scenescreenshots']");
            foreach (var sceneImage in sceneImages)
            {
                result.Add(new RemoteImageInfo
                {
                    Url = sceneImage.Attributes["href"].Value,
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
