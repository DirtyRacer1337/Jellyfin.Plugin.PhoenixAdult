using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
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
    public class NetworkGammaEnt : IProviderBase
    {
        public static async Task<string> GetAPIKey(string url, CancellationToken cancellationToken)
        {
            var http = await HTTP.Request(url + "/en/login", cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                var regEx = Regex.Match(http.Content, "\"apiKey\":\"(.*?)\"");
                if (regEx.Groups.Count > 0)
                {
                    return regEx.Groups[1].Value;
                }
            }

            return string.Empty;
        }

        public static async Task<JObject> GetDataFromAPI(string url, string indexName, string referer, string searchParams, CancellationToken cancellationToken)
        {
            JObject json = null;

            var param = $"{{'requests':[{{'indexName':'{indexName}','params':'{searchParams}'}}]}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Referer",  referer },
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

            var searchSceneID = searchTitle.Split()[0];
            if (!int.TryParse(searchSceneID, out _))
            {
                searchSceneID = null;
            }

            string apiKEY = await GetAPIKey(Helper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
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

                    curID = $"{siteNum[0]}#{siteNum[1]}#{sceneType}#{sceneID}";

                    if (sceneDateObj.HasValue)
                    {
                        var sceneDate = sceneDateObj.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        curID += $"#{sceneDate}";
                        res.PremiereDate = sceneDateObj;
                    }

                    res.ProviderIds.Add(Plugin.Instance.Name, curID);

                    if (searchResult.ContainsKey("pictures"))
                    {
                        var images = searchResult["pictures"].Where(item => !item.ToString().Contains("resized", StringComparison.OrdinalIgnoreCase));
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

            string apiKEY = await GetAPIKey(Helper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[0] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{Helper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[0]}", Helper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[1]}", cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["results"].First["hits"].First;

            string domain = new Uri(Helper.GetSearchBaseURL(siteNum)).Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase),
                sceneTypeURL = sceneID[1] == "scenes" ? "video" : "movie";

            if (sceneTypeURL.Equals("movie", StringComparison.OrdinalIgnoreCase))
            {
                switch (domain)
                {
                    case "freetour.adulttime.com":
                        sceneTypeURL = string.Empty;
                        break;

                    case "burningangel.com":
                    case "devilsfilm.com":
                    case "roccosiffredi.com":
                    case "genderx.com":
                        sceneTypeURL = "dvd";
                        break;
                }
            }

            var sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/en/{sceneTypeURL}/0/{sceneID[1]}/";

            if (!string.IsNullOrWhiteSpace(sceneTypeURL))
            {
                result.Item.ExternalId = sceneURL;
            }

            result.Item.Name = (string)sceneData["title"];
            var description = (string)sceneData["description"];
            result.Item.Overview = description.Replace("</br>", "\n", StringComparison.OrdinalIgnoreCase);
            result.Item.AddStudio((string)sceneData["network_name"]);

            if (DateTime.TryParseExact(sceneID[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
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

            string apiKEY = await GetAPIKey(Helper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[0] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{Helper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[0]}", Helper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[1]}", cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["results"].First["hits"].First;

            var ignore = false;
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

            string image = sceneData["url_title"].ToString().ToLowerInvariant().Replace('-', '_'),
                   imageURL = $"https://images-fame.gammacdn.com/movies/{sceneData["movie_id"]}/{sceneData["movie_id"]}_{image}_front_400x625.jpg";

            if (!ignore)
            {
                result.Add(new RemoteImageInfo
                {
                    Url = imageURL,
                    Type = ImageType.Primary,
                });
            }

            if (sceneData.ContainsKey("pictures"))
            {
                image = (string)sceneData["pictures"].Last(o => !o.ToString().Contains("resized", StringComparison.OrdinalIgnoreCase));
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
