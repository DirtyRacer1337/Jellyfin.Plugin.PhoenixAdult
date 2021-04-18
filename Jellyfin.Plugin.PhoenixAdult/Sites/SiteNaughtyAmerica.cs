using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
    public class SiteNaughtyAmerica : IProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchData, CancellationToken cancellationToken)
        {
            JObject json = null;

            var text = $"{{'requests':[{{'indexName':'nacms_combined_production','params':'{searchData}&hitsPerPage=100'}}]}}".Replace('\'', '"');
            var param = new StringContent(text, Encoding.UTF8, "application/json");

            var http = await HTTP.Request(url, HttpMethod.Post, param, cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                json = JObject.Parse(http.Content);
            }

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            JObject searchResults;
            var searchSceneID = searchTitle.Split()[0];
            string searchParams;
            if (int.TryParse(searchSceneID, out _))
            {
                searchParams = $"filters=id={searchSceneID}";
            }
            else
            {
                searchParams = $"query={searchTitle}";
            }

            var url = Helper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            searchResults = await GetDataFromAPI(url, searchParams, cancellationToken).ConfigureAwait(false);

            if (searchResults == null)
            {
                return result;
            }

            foreach (var searchResult in searchResults["results"].First["hits"])
            {
                string sceneIDs = (string)searchResult["id"],
                    curID = sceneIDs,
                    sceneName = (string)searchResult["title"];
                var sceneDate = (long)searchResult["published_at"];
                var sceneID = new string[] { curID };

                var posters = (await this.GetImages(siteNum, sceneID, null, cancellationToken).ConfigureAwait(false)).Where(o => o.Type == ImageType.Primary);

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    PremiereDate = DateTimeOffset.FromUnixTimeSeconds(sceneDate).DateTime,
                };

                if (posters.Any())
                {
                    res.ImageUrl = posters.First().Url;
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

            var url = Helper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            var sceneData = await GetDataFromAPI(url, $"filters=id={sceneID[0]}", cancellationToken).ConfigureAwait(false);

            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["results"].First["hits"].First;

            var sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/scene/0{sceneID[0]}";
            result.Item.ExternalId = sceneURL;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["synopsis"];
            result.Item.AddStudio("Naughty America");
            if (sceneData.ContainsKey("site"))
            {
                result.Item.AddStudio((string)sceneData["site"]);
            }

            var sceneDateObj = DateTimeOffset.FromUnixTimeSeconds((long)sceneData["published_at"]);
            result.Item.PremiereDate = sceneDateObj.DateTime;

            foreach (var genreLink in sceneData["fantasies"])
            {
                var genreName = (string)genreLink;

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["performers"])
            {
                string actorName = (string)actorLink,
                        actorsPageURL;

                actorsPageURL = actorName.ToLowerInvariant()
                    .Replace(" ", "-", StringComparison.OrdinalIgnoreCase)
                    .Replace("'", string.Empty, StringComparison.OrdinalIgnoreCase);

                var actorURL = $"https://www.naughtyamerica.com/pornstar/{actorsPageURL}";
                var actorData = await HTML.ElementFromURL(actorURL, cancellationToken).ConfigureAwait(false);
                var actorPhoto = actorData.SelectSingleText("//img[@class='performer-pic']/@src");

                var res = new PersonInfo
                {
                    Name = actorName,
                };

                if (!string.IsNullOrEmpty(actorPhoto))
                {
                    res.ImageUrl = $"https:" + actorPhoto;
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

            var sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/scene/0{sceneID[0]}";
            var sceneDataHTML = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var images = sceneDataHTML.SelectNodesSafe("//div[contains(@class, 'contain-scene-images') and contains(@class, 'desktop-only')]/a");
            foreach (var sceneImages in images)
            {
                var image = $"https:{sceneImages.Attributes["href"].Value}";
                result.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Primary,
                });
                result.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
