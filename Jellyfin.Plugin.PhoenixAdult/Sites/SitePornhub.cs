using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class SitePornhub : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            if ((searchTitle.StartsWith("ph", StringComparison.OrdinalIgnoreCase) || int.TryParse(searchTitle, out _)) && !searchTitle.Contains(' ', StringComparison.OrdinalIgnoreCase))
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + $"/view_video.php?viewkey={searchTitle}");
                var sceneID = new string[] { Helper.Encode(sceneURL.PathAndQuery) };

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, searchDate, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);
                }
            }
            else
            {
                searchTitle = searchTitle.Replace(" ", "+", StringComparison.OrdinalIgnoreCase);
                var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodesSafe("//ul[@id='videoSearchResult']/li[@data-video-vkey]");
                foreach (var searchResult in searchResults)
                {
                    var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//a/@href"));
                    string curID = Helper.Encode(sceneURL.PathAndQuery),
                        sceneName = searchResult.SelectSingleText(".//span[@class='title']"),
                        scenePoster = searchResult.SelectSingleText(".//div[@class='phimage']//img/@data-thumb_url");

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster,
                    };

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

            var http = await HTTP.Request(sceneURL, HttpMethod.Post, cancellationToken).ConfigureAwait(false);
            var sceneData = HTML.ElementFromStream(http.ContentStream);
            var json = sceneData.SelectSingleText("//script[@type='application/ld+json']");
            JObject sceneDataJSON = null;
            if (!string.IsNullOrEmpty(json))
            {
                sceneDataJSON = JObject.Parse(json);
            }

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//h1[@class='title']");
            var studioName = sceneData.SelectSingleText("//div[@class='userInfo']//a");
            result.Item.AddStudio("Pornhub");

            if (!string.IsNullOrEmpty(studioName))
            {
                result.Item.AddStudio(studioName);
            }

            if (sceneDataJSON != null)
            {
                var date = (string)sceneDataJSON["uploadDate"];
                if (date != null)
                {
                    if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                    {
                        result.Item.PremiereDate = sceneDateObj;
                    }
                }
            }

            var genreNode = sceneData.SelectNodesSafe("(//div[@class='categoriesWrapper'] | //div[@class='tagsWrapper'])/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[contains(@class, 'pornstarsWrapper')]/a");
            foreach (var actorLink in actorsNode)
            {
                string actorName = actorLink.Attributes["data-mxptext"].Value,
                        actorPhotoURL = actorLink.SelectSingleText(".//img[@class='avatar']/@src");

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

            var http = await HTTP.Request(sceneURL, HttpMethod.Post, cancellationToken).ConfigureAwait(false);
            var sceneData = HTML.ElementFromStream(http.ContentStream);

            var img = sceneData.SelectSingleText("//div[@id='player']//img/@src");
            if (!string.IsNullOrEmpty(img))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Primary,
                });
            }

            return result;
        }
    }
}
