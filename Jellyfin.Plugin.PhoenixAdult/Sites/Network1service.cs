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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class Network1service : IProviderBase
    {
        public static async Task<string> GetToken(int[] siteNum, CancellationToken cancellationToken)
        {
            var result = string.Empty;

            if (siteNum == null)
            {
                return result;
            }

            var db = new JObject();
            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.TokenStorage))
            {
                db = JObject.Parse(Plugin.Instance.Configuration.TokenStorage);
            }

            var keyName = new Uri(Helper.GetSearchBaseURL(siteNum)).Host;
            if (db.ContainsKey(keyName))
            {
                string token = (string)db[keyName],
                    res = Encoding.UTF8.GetString(Helper.ConvertFromBase64String(token.Split('.')[1]) ?? Array.Empty<byte>());

                if ((int)JObject.Parse(res)["exp"] > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    result = token;
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                var http = await HTTP.Request(Helper.GetSearchBaseURL(siteNum), HttpMethod.Head, cancellationToken).ConfigureAwait(false);
                var instanceToken = http.Cookies?.Where(o => o.Name == "instance_token");
                if (instanceToken == null || !instanceToken.Any())
                {
                    return result;
                }

                result = instanceToken.First().Value;

                if (db.ContainsKey(keyName))
                {
                    db[keyName] = result;
                }
                else
                {
                    db.Add(keyName, result);
                }

                Plugin.Instance.Configuration.TokenStorage = JsonConvert.SerializeObject(db);
                Plugin.Instance.SaveConfiguration();
            }

            return result;
        }

        public static async Task<JObject> GetDataFromAPI(string url, string instance, CancellationToken cancellationToken)
        {
            JObject json = null;
            var headers = new Dictionary<string, string>
            {
                { "Instance", instance },
            };

            var http = await HTTP.Request(url, cancellationToken, headers).ConfigureAwait(false);
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

            var searchSceneID = searchTitle.Split()[0];
            var sceneTypes = new List<string> { "scene", "movie", "serie" };
            if (!int.TryParse(searchSceneID, out _))
            {
                searchSceneID = null;
            }

            var instanceToken = await GetToken(siteNum, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(instanceToken))
            {
                return result;
            }

            foreach (var sceneType in sceneTypes)
            {
                string url;
                if (string.IsNullOrEmpty(searchSceneID))
                {
                    url = $"/v2/releases?type={sceneType}&search={searchTitle}";
                }
                else
                {
                    url = $"/v2/releases?type={sceneType}&id={searchSceneID}";
                }

                var searchResults = await GetDataFromAPI(Helper.GetSearchSearchURL(siteNum) + url, instanceToken, cancellationToken).ConfigureAwait(false);
                if (searchResults == null)
                {
                    break;
                }

                foreach (var searchResult in searchResults["result"])
                {
                    string sceneID = (string)searchResult["id"],
                            curID = $"{sceneID}#{sceneType}",
                            sceneName = (string)searchResult["title"],
                            scenePoster = string.Empty;
                    var sceneDateObj = (DateTime)searchResult["dateReleased"];

                    var imageTypes = new List<string> { "poster", "cover" };
                    foreach (var imageType in imageTypes)
                    {
                        if (searchResult["images"].Type == JTokenType.Object && searchResult["images"][imageType] != null)
                        {
                            foreach (JProperty image in searchResult["images"][imageType])
                            {
                                if (int.TryParse(image.Name, out _))
                                {
                                    scenePoster = (string)searchResult["images"][imageType][image.Name]["xx"]["url"];
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(scenePoster))
                        {
                            break;
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

            var instanceToken = await GetToken(siteNum, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(instanceToken))
            {
                return result;
            }

            var url = $"{Helper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[1]}&id={sceneID[0]}";
            var sceneData = await GetDataFromAPI(url, instanceToken, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["result"].First;
            if (sceneData == null)
            {
                return result;
            }

            string domain = new Uri(Helper.GetSearchBaseURL(siteNum)).Host,
                sceneTypeURL = sceneID[1];

            switch (domain)
            {
                case "www.brazzers.com":
                    if (sceneTypeURL.Equals("serie", StringComparison.OrdinalIgnoreCase) || sceneTypeURL.Equals("scene", StringComparison.OrdinalIgnoreCase))
                    {
                        sceneTypeURL = "video";
                    }

                    break;
            }

            var sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/{sceneTypeURL}/{sceneID[0]}/0";

            result.Item.ExternalId = sceneURL;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio((string)sceneData["brand"]);
            if (sceneData.ContainsKey("collections") && sceneData["collections"].Type == JTokenType.Array)
            {
                foreach (var collection in sceneData["collections"])
                {
                    result.Item.AddStudio((string)collection["name"]);
                }
            }

            var sceneDateObj = (DateTime)sceneData["dateReleased"];
            result.Item.PremiereDate = sceneDateObj;

            foreach (var genreLink in sceneData["tags"])
            {
                var genreName = (string)genreLink["name"];

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                var actorPageURL = $"{Helper.GetSearchSearchURL(siteNum)}/v1/actors?id={actorLink["id"]}";
                var actorData = await GetDataFromAPI(actorPageURL, instanceToken, cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorData = (JObject)actorData["result"].First;

                    var actor = new PersonInfo
                    {
                        Name = (string)actorLink["name"],
                    };

                    if (actorData["images"] != null && actorData["images"].Type == JTokenType.Object)
                    {
                        actor.ImageUrl = (string)actorData["images"]["profile"]["0"]["xs"]["url"];
                    }

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

            var instanceToken = await GetToken(siteNum, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(instanceToken))
            {
                return result;
            }

            var url = $"{Helper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[1]}&id={sceneID[0]}";
            var sceneData = await GetDataFromAPI(url, instanceToken, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["result"].First;

            var imageTypes = new List<string> { "poster", "cover" };
            foreach (var imageType in imageTypes)
            {
                if (sceneData["images"].Type == JTokenType.Object && sceneData["images"][imageType] != null)
                {
                    foreach (JProperty image in sceneData["images"][imageType])
                    {
                        if (int.TryParse(image.Name, out _))
                        {
                            result.Add(new RemoteImageInfo
                            {
                                Url = (string)sceneData["images"][imageType][image.Name]["xx"]["url"],
                                Type = ImageType.Primary,
                            });
                            result.Add(new RemoteImageInfo
                            {
                                Url = (string)sceneData["images"][imageType][image.Name]["xx"]["url"],
                                Type = ImageType.Backdrop,
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}
