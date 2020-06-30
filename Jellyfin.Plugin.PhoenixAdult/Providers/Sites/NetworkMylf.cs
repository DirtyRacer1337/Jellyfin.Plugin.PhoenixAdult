using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
{
    internal class NetworkMylf : IPhoenixAdultProviderBase
    {
        public static async Task<JObject> GetJSONfromPage(string url, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await url.AllowAnyHttpStatus().GetAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
            {
                var data = await http.Content.ReadAsStringAsync().ConfigureAwait(false);
                var regEx = new Regex(@"window\.__INITIAL_STATE__ = (.*);").Match(data);
                if (regEx.Groups.Count > 0)
                    json = (JObject)JObject.Parse(regEx.Groups[1].Value)["content"];
            }

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLower(PhoenixAdultHelper.Lang);
            if (!directURL.Contains("/", StringComparison.OrdinalIgnoreCase))
                directURL = PhoenixAdultHelper.ReplaceFirst(directURL, "-", "/");

            if (!int.TryParse(directURL.Split("/")[0], out _))
            {
                directURL = PhoenixAdultHelper.ReplaceFirst(directURL, "/", "-");
            }
            else
                directURL = directURL.Split("/")[1];

            directURL = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + directURL;
            var searchResultsURLs = new List<string>
            {
                directURL
            };

            var searchResults = await PhoenixAdultHelper.GetGoogleSearchResults(searchTitle, siteNum, cancellationToken).ConfigureAwait(false);

            foreach (var searchResult in searchResults)
            {
                var url = searchResult.Split("?").First();
                if (url.Contains("/movies/", StringComparison.OrdinalIgnoreCase) && !searchResultsURLs.Contains(url))
                    searchResultsURLs.Add(url);
            }

            foreach (var sceneURL in searchResultsURLs)
            {
                string curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);
                if (sceneData != null)
                {
                    var res = new RemoteSearchResult
                    {
                        Name = sceneData.Item.Name
                    };

                    if (searchDate.HasValue)
                    {
                        res.PremiereDate = searchDate;
                        curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultHelper.Lang)}";
                    }

                    res.ProviderIds.Add(PhoenixAdultProvider.PluginName, curID);
                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie()
            };
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return null;

            string contentName = string.Empty;
            foreach (var name in new List<string>() { "moviesContent", "videosContent" })
                if (sceneData.ContainsKey(name) && (sceneData[name] != null))
                {
                    contentName = name;
                    break;
                }

            if (string.IsNullOrEmpty(contentName))
                return null;

            sceneData = (JObject)sceneData[contentName];
            var sceneName = sceneData.Properties().First().Name;
            sceneData = (JObject)sceneData[sceneName];

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio("Mylf");

            DateTime? releaseDate = null;
            if (sceneData.ContainsKey("publishedDate"))
                releaseDate = (DateTime)sceneData["publishedDate"];
            else
            {
                if (sceneID.Length > 3)
                    if (DateTime.TryParseExact(sceneID[3], "yyyy-MM-dd", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                        releaseDate = sceneDateObj;
            }

            if (releaseDate.HasValue)
            {
                result.Item.PremiereDate = releaseDate.Value;
                result.Item.ProductionYear = releaseDate.Value.Year;
            }

            string subSite;
            if (sceneData.ContainsKey("site"))
                subSite = (string)sceneData["site"]["name"];
            else
                subSite = PhoenixAdultHelper.GetSearchSiteName(siteNum);

            var genres = new List<string>();
            switch (subSite)
            {
                case "MylfBoss":
                    genres = new List<string> {
                        "Office", "Boss"
                    };
                    break;
                case "MylfBlows":
                    genres = new List<string> {
                        "Blowjob"
                    };
                    break;
                case "Milfty":
                    genres = new List<string> {
                        "Cheating"
                    };
                    break;
                case "Mom Drips":
                    genres = new List<string> {
                        "Creampie"
                    };
                    break;
                case "Milf Body":
                    genres = new List<string> {
                        "Gym", "Fitness"
                    };
                    break;
                case "Lone Milf":
                    genres = new List<string> {
                        "Solo"
                    };
                    break;
                case "Full Of JOI":
                    genres = new List<string> {
                        "JOI"
                    };
                    break;
                case "Mylfed":
                    genres = new List<string> {
                        "Lesbian"
                    };
                    break;
                case "MylfDom":
                    genres = new List<string> {
                        "BDSM"
                    };
                    break;
            }

            foreach (var genreName in genres)
                result.Item.AddGenre(genreName);

            foreach (var genreName in new List<string>() { "MILF", "Mature" })
                result.Item.AddGenre(genreName);

            foreach (var actorLink in sceneData["models"])
            {
                string actorName = (string)actorLink["modelName"],
                       actorID = (string)actorLink["modelId"],
                       actorPhotoURL;

                var actorData = await GetJSONfromPage($"{PhoenixAdultHelper.GetSearchBaseURL(siteNum)}/models/{actorID}", cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorPhotoURL = (string)actorData["modelsContent"][actorID]["img"];

                    result.AddPerson(new PersonInfo
                    {
                        Name = actorName,
                        ImageUrl = actorPhotoURL,
                        Type = PersonType.Actor
                    });
                }
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();
            if (item == null)
                return images;

            string[] sceneID = item.ProviderIds[PhoenixAdultProvider.PluginName].Split('#');

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return images;

            foreach (var contentName in new List<string>() { "moviesContent", "videosContent" })
                if (sceneData.ContainsKey(contentName) && (sceneData[contentName] != null))
                {
                    sceneData = (JObject)sceneData[contentName];
                    var sceneName = sceneData.Properties().First().Name;
                    sceneData = (JObject)sceneData[sceneName];
                }

            var img = (string)sceneData["img"];

            images.Add(new RemoteImageInfo
            {
                Url = img,
                Type = ImageType.Primary,
                ProviderName = PhoenixAdultProvider.PluginName
            });

            images.Add(new RemoteImageInfo
            {
                Url = img,
                Type = ImageType.Backdrop,
                ProviderName = PhoenixAdultProvider.PluginName
            });

            return images;
        }
    }
}
