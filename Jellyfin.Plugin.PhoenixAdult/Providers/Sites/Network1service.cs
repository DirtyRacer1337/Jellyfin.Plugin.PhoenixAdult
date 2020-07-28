using System;
using System.Collections.Generic;
using System.Net;
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
    internal class Network1service : IPhoenixAdultProviderBase
    {
        public static async Task<IDictionary<string, Cookie>> GetCookies(string url, CancellationToken cancellationToken)
        {
            IDictionary<string, Cookie> cookies;

            using (var http = new FlurlClient(url))
            {
                await http.EnableCookies().AllowAnyHttpStatus().Request().HeadAsync(cancellationToken).ConfigureAwait(false);
                cookies = http.Cookies;
            }

            return cookies;
        }

        public static async Task<JObject> GetDataFromAPI(string url, string instance, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await url.AllowAnyHttpStatus().WithHeader("Instance", instance).GetAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
                json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var searchSceneID = searchTitle.Split()[0];
            var sceneTypes = new List<string> { "scene", "movie", "serie" };
            if (!int.TryParse(searchSceneID, out _))
                searchSceneID = null;

            var cookies = await GetCookies(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return result;

            foreach (var sceneType in sceneTypes)
            {
                string url;
                if (string.IsNullOrEmpty(searchSceneID))
                    url = $"/v2/releases?type={sceneType}&search={searchTitle}";
                else
                    url = $"/v2/releases?type={sceneType}&id={searchSceneID}";

                var searchResults = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum) + url, cookie.Value, cancellationToken).ConfigureAwait(false);
                if (searchResults == null)
                    break;

                foreach (var searchResult in searchResults["result"])
                {
                    string sceneID = (string)searchResult["id"],
                            curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}#{sceneType}",
                            sceneName = (string)searchResult["title"],
                            scenePoster = string.Empty;
                    DateTime sceneDateObj = (DateTime)searchResult["dateReleased"];

                    var imageTypes = new List<string> { "poster", "cover" };
                    foreach (var imageType in imageTypes)
                        if (searchResult["images"][imageType] != null)
                            foreach (var image in searchResult["images"][imageType])
                                scenePoster = (string)image["xx"]["url"];

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster,
                        PremiereDate = sceneDateObj
                    };

                    result.Add(res);
                }
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
                return result;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            var cookies = await GetCookies(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return result;

            var url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[3]}&id={sceneID[2]}";
            var sceneData = await GetDataFromAPI(url, cookie.Value, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return result;

            sceneData = (JObject)sceneData["result"].First;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio(PhoenixAdultProvider.Lang.TextInfo.ToTitleCase((string)sceneData["brand"]));

            DateTime sceneDateObj = (DateTime)sceneData["dateReleased"];
            result.Item.PremiereDate = sceneDateObj;

            foreach (var genreLink in sceneData["tags"])
            {
                var genreName = (string)genreLink["name"];

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                var actorPageURL = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}/v1/actors?id={actorLink["id"]}";
                var actorData = await GetDataFromAPI(actorPageURL, cookie.Value, cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorData = (JObject)actorData["result"].First;

                    var actor = new PersonInfo
                    {
                        Name = (string)actorLink["name"]
                    };

                    if (actorData["images"] != null && actorData["images"].Type == JTokenType.Object)
                        actor.ImageUrl = (string)actorData["images"]["profile"].First["xs"]["url"];

                    result.People.Add(actor);
                }
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            var cookies = await GetCookies(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return result;

            var url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[3]}&id={sceneID[2]}";
            var sceneData = await GetDataFromAPI(url, cookie.Value, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return result;

            sceneData = (JObject)sceneData["result"].First;

            var imageTypes = new List<string> { "poster", "cover" };
            foreach (var imageType in imageTypes)
                if (sceneData["images"][imageType] != null)
                    foreach (var image in sceneData["images"][imageType])
                    {
                        result.Add(new RemoteImageInfo
                        {
                            Url = (string)image["xx"]["url"],
                            Type = ImageType.Primary
                        });
                        result.Add(new RemoteImageInfo
                        {
                            Url = (string)image["xx"]["url"],
                            Type = ImageType.Backdrop
                        });
                    }

            return result;
        }
    }
}
