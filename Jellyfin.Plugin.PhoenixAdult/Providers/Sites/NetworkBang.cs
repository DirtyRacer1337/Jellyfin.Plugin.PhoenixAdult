using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Providers.Helpers;

namespace PhoenixAdult.Providers.Sites
{
    internal class NetworkBang : IPhoenixAdultProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchTitle, string searchType, CancellationToken cancellationToken)
        {
            var param = $"{{'query':{{'bool':{{'must':[{{'match':{{'{searchType}':'{searchTitle}'}}}},{{'match':{{'type':'movie'}}}}],'must_not':[{{'match':{{'type':'trailer'}}}}]}}}}}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Authorization", "Basic YmFuZy1yZWFkOktqVDN0RzJacmQ1TFNRazI=" },
                {"Content-Type", "application/json" }
            };

            var http = await url.WithHeaders(headers).PostStringAsync(param, cancellationToken).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            JObject searchResults;
            var searchSceneID = searchTitle.Split()[0];
            if (int.TryParse(searchSceneID, out _))
                searchResults = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), searchSceneID, "identifier", cancellationToken).ConfigureAwait(false);
            else
                searchResults = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), searchTitle, "name", cancellationToken).ConfigureAwait(false);

            foreach (var searchResult in searchResults["hits"]["hits"])
            {
                var sceneData = searchResult["_source"];
                string sceneID = (string)sceneData["identifier"],
                        curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}",
                        sceneName = (string)sceneData["name"],
                        scenePoster = $"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg";
                DateTime sceneDateObj = (DateTime)sceneData["releaseDate"];

                var item = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                    PremiereDate = sceneDateObj
                };

                result.Add(item);
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>()
            };

            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            var sceneData = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), sceneID[2], "identifier", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["hits"]["hits"].First["_source"];

            result.Item.Name = (string)sceneData["name"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio(CultureInfo.InvariantCulture.TextInfo.ToTitleCase((string)sceneData["studio"]["name"]));

            DateTime sceneDateObj = (DateTime)sceneData["releaseDate"];
            result.Item.PremiereDate = sceneDateObj;

            foreach (var genreLink in sceneData["genres"])
            {
                var genreName = (string)genreLink["name"];

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                string actorName = (string)actorLink["name"],
                       actorPhoto = $"https://i.bang.com/pornstars/{actorLink["id"]}.jpg";

                result.People.Add(new PersonInfo
                {
                    Name = actorName,
                    ImageUrl = actorPhoto
                });
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            var sceneData = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), sceneID[2], "identifier", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["hits"]["hits"].First["_source"];

            result.Add(new RemoteImageInfo
            {
                Url = $"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg",
                Type = ImageType.Primary
            });

            foreach (var image in sceneData["screenshots"])
                result.Add(new RemoteImageInfo
                {
                    Url = $"https://i.bang.com/screenshots/{sceneData["dvd"]["id"]}/movie/1/{image["screenId"]}.jpg",
                    Type = ImageType.Backdrop
                });

            return result;
        }
    }
}
