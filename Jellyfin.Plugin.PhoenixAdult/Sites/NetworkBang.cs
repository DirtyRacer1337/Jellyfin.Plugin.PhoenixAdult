using System;
using System.Collections.Generic;
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
    public class NetworkBang : IProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchTitle, string searchType, CancellationToken cancellationToken)
        {
            JObject json = null;

            var param = $"{{'query':{{'bool':{{'must':[{{'match':{{'{searchType}':'{searchTitle}'}}}},{{'match':{{'type':'movie'}}}}],'must_not':[{{'match':{{'type':'trailer'}}}}]}}}}}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Basic YmFuZy1yZWFkOktqVDN0RzJacmQ1TFNRazI=" },
                { "Content-Type", "application/json" },
            };

            var http = await HTTP.Request(url, HTTP.CreateRequest(param, headers), cancellationToken).ConfigureAwait(false);
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
            if (int.TryParse(searchSceneID, out _))
            {
                searchResults = await GetDataFromAPI(Helper.GetSearchSearchURL(siteNum), searchSceneID, "identifier", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                searchResults = await GetDataFromAPI(Helper.GetSearchSearchURL(siteNum), searchTitle, "name", cancellationToken).ConfigureAwait(false);
            }

            if (searchResults == null)
            {
                return result;
            }

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
                    PremiereDate = sceneDateObj,
                };

                result.Add(item);
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

            var sceneData = await GetDataFromAPI(Helper.GetSearchSearchURL(siteNum), sceneID[0], "identifier", cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["hits"]["hits"].First;

            // Don't know where get id
            // result.Item.ExternalId = Helper.GetSearchBaseURL(siteNum) + $"/{(string)sceneData["_type"]}/{(string)sceneData["_id"]}/";
            sceneData = (JObject)sceneData["_source"];

            result.Item.Name = (string)sceneData["name"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio((string)sceneData["studio"]["name"]);

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

            var sceneData = await GetDataFromAPI(Helper.GetSearchSearchURL(siteNum), sceneID[0], "identifier", cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["hits"]["hits"].First["_source"];
            result.Add(new RemoteImageInfo
            {
                Url = $"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg",
                Type = ImageType.Primary,
            });

            foreach (var image in sceneData["screenshots"])
            {
                result.Add(new RemoteImageInfo
                {
                    Url = $"https://i.bang.com/screenshots/{sceneData["dvd"]["id"]}/movie/1/{image["screenId"]}.jpg",
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
