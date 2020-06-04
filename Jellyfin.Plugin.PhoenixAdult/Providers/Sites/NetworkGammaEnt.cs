using System;
using System.Collections.Generic;
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
    class NetworkGammaEnt : IPhoenixAdultProviderBase
    {
        public static async Task<string> GetAPIKey(string url, CancellationToken cancellationToken)
        {
            var http = await url.GetAsync(cancellationToken).ConfigureAwait(false);
            var regEx = Regex.Match(await http.Content.ReadAsStringAsync().ConfigureAwait(false), "\"apiKey\":\"(.*?)\"");
            if (regEx.Groups.Count > 0)
                return regEx.Groups[1].Value;

            return string.Empty;
        }

        public static async Task<JObject> GetDataFromAPI(string url, string indexName, string referer, string searchParams, CancellationToken cancellationToken)
        {
            var param = $"{{'requests':[{{'indexName':'{indexName}','params':'{searchParams}'}}]}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "application/json" },
                {"Referer",  referer},
            };

            var http = await url.AllowAnyHttpStatus().WithHeaders(headers).PostStringAsync(param, cancellationToken).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, string searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var searchSceneID = searchTitle.Split()[0];
            var sceneTypes = new List<string> { "scenes", "movies" };
            if (!int.TryParse(searchSceneID, out _))
                searchSceneID = null;

            string apiKEY = await GetAPIKey(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                searchParams;

            foreach (var sceneType in sceneTypes)
            {
                if (!string.IsNullOrEmpty(searchSceneID))
                    if (sceneType == "scenes")
                        searchParams = $"filters=clip_id={searchSceneID}";
                    else
                        searchParams = $"filters=movie_id={searchSceneID}";
                else
                    searchParams = $"query={searchTitle}";

                var url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
                var searchResults = await GetDataFromAPI(url, $"all_{sceneType}", PhoenixAdultHelper.GetSearchBaseURL(siteNum), searchParams, cancellationToken).ConfigureAwait(false);

                foreach (var searchResult in searchResults["results"].First["hits"])
                {
                    string sceneID,
                            curID,
                            sceneName = (string)searchResult["title"],
                            sceneDate;

                    if (sceneType == "scenes")
                    {
                        sceneDate = (string)searchResult["release_date"];
                        sceneID = (string)searchResult["clip_id"];
                    }
                    else
                    {
                        var dateField = searchResult["last_modified"] != null ? "last_modified" : "date_created";
                        sceneDate = (string)searchResult[dateField];
                        sceneID = (string)searchResult["movie_id"];
                    }

                    curID = $"{siteNum[0]}#{siteNum[1]}#{sceneType}#{sceneID}#{sceneDate}";

                    result.Add(new RemoteSearchResult
                    {
                        ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                        Name = sceneName
                    });
                    if (DateTime.TryParse(sceneDate, out DateTime sceneDateObj))
                        result.Last().PremiereDate = sceneDateObj;
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
                return result;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            string apiKEY = await GetAPIKey(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[2] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[2]}", PhoenixAdultHelper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[3]}", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["results"].First["hits"].First;

            result.Item.Name = (string)sceneData["title"];
            var description = (string)sceneData["description"];
            result.Item.Overview = description.Replace("</br>", "\n", StringComparison.OrdinalIgnoreCase);
            result.Item.AddStudio(PhoenixAdultHelper.Lang.TextInfo.ToTitleCase((string)sceneData["network_name"]));

            if (DateTime.TryParse(sceneID[4], out DateTime sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
                result.Item.ProductionYear = sceneDateObj.Year;
            }

            foreach (var genreLink in sceneData["categories"])
            {
                var genreName = (string)genreLink["name"];

                if (!string.IsNullOrEmpty(genreName))
                    result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                string actorName = (string)actorLink["name"],
                       actorPhotoURL = string.Empty;

                var data = await GetDataFromAPI(url, "all_actors", PhoenixAdultHelper.GetSearchBaseURL(siteNum), $"filters=actor_id={actorLink["actor_id"]}", cancellationToken).ConfigureAwait(false);
                var actorData = data["results"].First["hits"].First;
                if (actorData["pictures"] != null)
                    actorPhotoURL = (string)actorData["pictures"].Last;

                result.AddPerson(new PersonInfo
                {
                    Name = actorName,
                    Type = PersonType.Actor
                });
                if (actorPhotoURL != null)
                    result.People.Last().ImageUrl = $"https://images-fame.gammacdn.com/actors{actorPhotoURL}";
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();
            if (item == null)
                return images;

            string[] sceneID = item.ProviderIds[PhoenixAdultProvider.PluginName].Split('#');
            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            string apiKEY = await GetAPIKey(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[2] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[2]}", PhoenixAdultHelper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[3]}", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["results"].First["hits"].First;

            var ignore = false;
            var siteList = new List<string>
            {
                "girlsway.com", "puretaboo.com"
            };
            foreach (var site in siteList)
                if (PhoenixAdultHelper.GetSearchBaseURL(siteNum).EndsWith(site, StringComparison.OrdinalIgnoreCase))
                {
                    ignore = true;
                    break;
                }

            string image = sceneData["url_title"].ToString().ToLower(PhoenixAdultHelper.Lang).Replace('-', '_'),
                   imageURL = $"https://images-fame.gammacdn.com/movies/{sceneData["movie_id"]}/{sceneData["movie_id"]}_{image}_front_400x625.jpg";


            if (!ignore)
                images.Add(new RemoteImageInfo
                {
                    Url = imageURL,
                    Type = ImageType.Primary,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

            image = (string)sceneData["pictures"].Last(item => !item.ToString().Contains("resized", StringComparison.OrdinalIgnoreCase));
            imageURL = $"https://images-fame.gammacdn.com/movies/{image}";
            images.Add(new RemoteImageInfo
            {
                Url = imageURL,
                Type = ImageType.Primary,
                ProviderName = PhoenixAdultProvider.PluginName
            });
            images.Add(new RemoteImageInfo
            {
                Url = imageURL,
                Type = ImageType.Backdrop,
                ProviderName = PhoenixAdultProvider.PluginName
            });

            return images;
        }
    }
}
