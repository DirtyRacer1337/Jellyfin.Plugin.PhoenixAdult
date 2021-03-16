using System;
using System.Collections.Generic;
using System.Linq;
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
    public class NetworkStrike3 : IProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await HTTP.Request(url, cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                json = (JObject)JObject.Parse(http.Content)["data"];
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

            var url = Helper.GetSearchSearchURL(siteNum) + $"/search?q={searchTitle}";
            var searchResults = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (searchResults == null)
            {
                return result;
            }

            foreach (var searchResult in searchResults["videos"])
            {
                string sceneURL = (string)searchResult["targetUrl"],
                        curID = Helper.Encode(sceneURL),
                        sceneName = (string)searchResult["title"],
                        scenePoster = string.Empty;
                var sceneDateObj = (DateTime)searchResult["releaseDate"];

                var imageTypes = new List<string> { "movie", "poster" };
                foreach (var imageType in imageTypes)
                {
                    if (searchResult["images"][imageType] != null && searchResult["images"][imageType].Any())
                    {
                        scenePoster = (string)searchResult["images"][imageType].First()["src"];
                    }
                }

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                    PremiereDate = sceneDateObj,
                };

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

            var url = Helper.GetSearchSearchURL(siteNum) + sceneURL;
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["video"];

            result.Item.ExternalId = Helper.GetSearchBaseURL(siteNum) + sceneURL;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio((string)sceneData["primarySite"]);

            var sceneDateObj = (DateTime)sceneData["releaseDate"];
            result.Item.PremiereDate = sceneDateObj;

            var genreTypes = new List<string> { "categories", "tags" };
            foreach (var genreType in genreTypes)
            {
                foreach (var genreLink in sceneData[genreType])
                {
                    string genreName;
                    if (genreLink.Type == JTokenType.Object)
                    {
                        genreName = (string)genreLink["name"];
                    }
                    else
                    {
                        genreName = (string)genreLink;
                    }

                    result.Item.AddGenre(genreName);
                }
            }

            foreach (var actorLink in sceneData["modelsSlugged"])
            {
                var actorPageURL = Helper.GetSearchSearchURL(siteNum) + $"/{actorLink["slugged"]}";
                var actorData = await GetDataFromAPI(actorPageURL, cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorData = (JObject)actorData["model"];

                    var actor = new PersonInfo
                    {
                        Name = (string)actorData["name"],
                        ImageUrl = (string)actorData["cdnUrl"],
                    };

                    result.People.Add(actor);
                }
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

            var url = Helper.GetSearchSearchURL(siteNum) + sceneURL;
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            var video = (JObject)sceneData["video"];

            var imageTypes = new List<string> { "movie", "poster" };
            foreach (var imageType in imageTypes)
            {
                if (video["images"][imageType] != null && video["images"][imageType].Any())
                {
                    JToken img;
                    var imgObj = (JObject)video["images"][imageType].Last();

                    if (imgObj.ContainsKey("highdpi"))
                    {
                        img = imgObj["highdpi"]["3x"];
                    }
                    else
                    {
                        img = imgObj["src"];
                    }

                    result.Add(new RemoteImageInfo
                    {
                        Url = (string)img,
                        Type = ImageType.Primary,
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = (string)img,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            var pictureSet = (JArray)sceneData["pictureset"];
            foreach (var image in pictureSet)
            {
                var img = (string)image["main"].First()["src"];

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
