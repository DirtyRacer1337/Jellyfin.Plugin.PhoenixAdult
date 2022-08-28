using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
    public class NetworkGammaEnt : IProviderBase
    {
        public static async Task<string> GetAPIKey(int[] siteNum, CancellationToken cancellationToken)
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
                    res = Encoding.UTF8.GetString(Helper.ConvertFromBase64String(token) ?? Array.Empty<byte>());

                if (res.Contains("validUntil") && int.TryParse(res.Split("validUntil=")[1].Split("&")[0], out var timestamp))
                {
                    if (timestamp > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    {
                        result = token;
                    }
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                var http = await HTTP.Request(Helper.GetSearchBaseURL(siteNum) + "/en/login", cancellationToken).ConfigureAwait(false);
                if (http.IsOK)
                {
                    var regEx = Regex.Match(http.Content, "\"apiKey\":\"(.*?)\"");
                    if (regEx.Groups.Count > 0)
                    {
                        result = regEx.Groups[1].Value;
                    }
                }

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

        public static async Task<JObject> GetDataFromAPI(string url, string indexName, string referer, string searchParams, CancellationToken cancellationToken)
        {
            JObject json = null;

            var text = $"{{'requests':[{{'indexName':'{indexName}','params':'{searchParams}'}}]}}".Replace('\'', '"');
            var param = new StringContent(text, Encoding.UTF8, "application/json");
            var headers = new Dictionary<string, string>
            {
                { "Referer",  referer },
            };

            var http = await HTTP.Request(url, HttpMethod.Post, param, cancellationToken, headers).ConfigureAwait(false);
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
            if (!int.TryParse(searchSceneID, out _))
            {
                searchSceneID = null;
            }

            string apiKEY = await GetAPIKey(siteNum, cancellationToken).ConfigureAwait(false),
                   searchParams;

            var sceneTypes = new List<string> { "scenes", "movies" };
            foreach (var sceneType in sceneTypes)
            {
                if (!string.IsNullOrEmpty(searchSceneID))
                {
                    if (sceneType == "scenes")
                    {
                        searchParams = $"filters=clip_id={searchSceneID}";
                    }
                    else
                    {
                        searchParams = $"filters=movie_id={searchSceneID}";
                    }
                }
                else
                {
                    searchParams = $"query={searchTitle}";
                }

                var url = $"{Helper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
                var searchResults = await GetDataFromAPI(url, $"all_{sceneType}", Helper.GetSearchBaseURL(siteNum), searchParams, cancellationToken).ConfigureAwait(false);

                if (searchResults == null)
                {
                    return result;
                }

                foreach (JObject searchResult in searchResults["results"].First["hits"])
                {
                    string sceneID,
                        sceneName = (string)searchResult["title"],
                        curID;
                    DateTime? sceneDateObj;

                    if (sceneType == "scenes")
                    {
                        sceneDateObj = (DateTime?)searchResult["release_date"];
                        sceneID = (string)searchResult["clip_id"];
                    }
                    else
                    {
                        var dateField = searchResult["last_modified"] != null ? "last_modified" : "date_created";
                        sceneDateObj = (DateTime?)searchResult[dateField];
                        sceneID = (string)searchResult["movie_id"];
                    }

                    var res = new RemoteSearchResult
                    {
                        Name = sceneName,
                    };

                    curID = $"{sceneID}#{sceneType}";

                    if (sceneDateObj.HasValue)
                    {
                        var sceneDate = sceneDateObj.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        curID += $"#{sceneDate}";
                        res.PremiereDate = sceneDateObj;
                    }

                    res.ProviderIds.Add(Plugin.Instance.Name, curID);

                    if (searchResult.ContainsKey("pictures"))
                    {
                        var images = searchResult["pictures"].Where(o => o.Type == JTokenType.Property);
                        if (images.Any())
                        {
                            res.ImageUrl = $"https://images-fame.gammacdn.com/movies/{(string)images.Last()}";
                        }
                    }

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

            string apiKEY = await GetAPIKey(siteNum, cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[1] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{Helper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[1]}", Helper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[0]}", cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["results"].First["hits"].First;

            string domain = new Uri(Helper.GetSearchBaseURL(siteNum)).Host,
                sceneTypeURL = sceneID[1] == "scenes" ? "video" : "movie";

            if (sceneTypeURL.Equals("movie", StringComparison.OrdinalIgnoreCase))
            {
                switch (domain)
                {
                    case "freetour.adulttime.com":
                        sceneTypeURL = string.Empty;
                        break;

                    case "www.burningangel.com":
                    case "www.devilsfilm.com":
                    case "www.roccosiffredi.com":
                    case "www.genderx.com":
                        sceneTypeURL = "dvd";
                        break;
                }
            }

            var sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/en/{sceneTypeURL}/0/{sceneID[0]}/";

            if (!string.IsNullOrWhiteSpace(sceneTypeURL))
            {
                result.Item.ExternalId = sceneURL;
            }

            result.Item.Name = (string)sceneData["title"];
            var description = (string)sceneData["description"];
            result.Item.Overview = description.Replace("</br>", "\n", StringComparison.OrdinalIgnoreCase);

            var network = (string)sceneData["network_name"];
            if (network != null)
            {
                result.Item.AddStudio(network);
            }

            if (sceneData.ContainsKey("studio_name"))
            {
                result.Item.AddStudio((string)sceneData["studio_name"]);
            }

            if (DateTime.TryParseExact(sceneID[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            foreach (var genreLink in sceneData["categories"])
            {
                var genreName = (string)genreLink["name"];

                if (!string.IsNullOrEmpty(genreName))
                {
                    result.Item.AddGenre(genreName);
                }
            }

            foreach (var actorLink in sceneData["actors"])
            {
                string actorName = (string)actorLink["name"],
                       actorPhotoURL = string.Empty;

                var data = await GetDataFromAPI(url, "all_actors", Helper.GetSearchBaseURL(siteNum), $"filters=actor_id={actorLink["actor_id"]}", cancellationToken).ConfigureAwait(false);
                if (data != null)
                {
                    var actorData = data["results"].First["hits"].First;
                    if (actorData["pictures"] != null)
                    {
                        actorPhotoURL = (string)actorData["pictures"].Last;
                    }

                    var actor = new PersonInfo
                    {
                        Name = actorName,
                    };

                    if (actorPhotoURL != null)
                    {
                        actor.ImageUrl = $"https://images-fame.gammacdn.com/actors{actorPhotoURL}";
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

            string apiKEY = await GetAPIKey(siteNum, cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[1] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{Helper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[1]}", Helper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[0]}", cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["results"].First["hits"].First;

            var ignore = false;
            string image, imageURL;

            if (sceneID[1].Equals("scenes", StringComparison.OrdinalIgnoreCase))
            {
                ignore = true;
            }
            else
            {
                var siteList = new List<string>
                {
                    "girlsway.com", "puretaboo.com",
                };

                foreach (var site in siteList)
                {
                    if (Helper.GetSearchBaseURL(siteNum).EndsWith(site, StringComparison.OrdinalIgnoreCase))
                    {
                        ignore = true;
                        break;
                    }
                }
            }

            if (!ignore)
            {
                image = (sceneData.ContainsKey("url_movie_title") ? sceneData["url_movie_title"] : sceneData["url_title"]).ToString().ToLowerInvariant().Replace('-', '_');
                imageURL = $"https://images-fame.gammacdn.com/movies/{sceneData["movie_id"]}/{sceneData["movie_id"]}_{image}_front_400x625.jpg";

                result.Add(new RemoteImageInfo
                {
                    Url = imageURL,
                    Type = ImageType.Primary,
                });
            }

            if (sceneData.ContainsKey("pictures"))
            {
                image = (string)sceneData["pictures"].Last(o => !o.ToString().Equals("resized", StringComparison.OrdinalIgnoreCase));
                imageURL = $"https://images-fame.gammacdn.com/movies/{image}";

                result.Add(new RemoteImageInfo
                {
                    Url = imageURL,
                    Type = ImageType.Primary,
                });
                result.Add(new RemoteImageInfo
                {
                    Url = imageURL,
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
