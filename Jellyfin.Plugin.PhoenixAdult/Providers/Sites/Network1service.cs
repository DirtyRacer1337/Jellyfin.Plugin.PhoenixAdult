using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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
    internal class Network1service : IPhoenixAdultProviderBase
    {
        public static async Task<IDictionary<string, Cookie>> GetCookies(string url, CancellationToken cancellationToken)
        {
            IDictionary<string, Cookie> cookies;
            var http = new FlurlClient(url).EnableCookies().AllowAnyHttpStatus();
            await http.Request().HeadAsync(cancellationToken).ConfigureAwait(false);
            cookies = http.Cookies;
            http.Dispose();

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
                            sceneName = (string)searchResult["title"];
                    DateTime sceneDateObj = (DateTime)searchResult["dateReleased"];

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                        Name = sceneName,
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
                Item = new Movie()
            };
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var cookies = await GetCookies(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return null;

            var url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[3]}&id={sceneID[2]}";
            var sceneData = await GetDataFromAPI(url, cookie.Value, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return null;

            sceneData = (JObject)sceneData["result"].First;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio(PhoenixAdultHelper.Lang.TextInfo.ToTitleCase((string)sceneData["brand"]));

            DateTime sceneDateObj = (DateTime)sceneData["dateReleased"];
            result.Item.PremiereDate = sceneDateObj;
            result.Item.ProductionYear = sceneDateObj.Year;

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

                    result.AddPerson(new PersonInfo
                    {
                        Name = (string)actorLink["name"],
                        Type = PersonType.Actor
                    });
                    if (actorData["images"] != null && actorData["images"].Type == JTokenType.Object)
                        result.People.Last().ImageUrl = (string)actorData["images"]["profile"].First["xs"]["url"];
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
            var siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var cookies = await GetCookies(PhoenixAdultHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return images;

            var imageTypes = new List<string> { "poster", "cover" };
            var url = $"{PhoenixAdultHelper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[3]}&id={sceneID[2]}";
            var sceneData = await GetDataFromAPI(url, cookie.Value, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return images;

            sceneData = (JObject)sceneData["result"].First;

            foreach (var imageType in imageTypes)
                if (sceneData["images"][imageType] != null)
                    foreach (var image in sceneData["images"][imageType])
                    {
                        images.Add(new RemoteImageInfo
                        {
                            Url = (string)image["xx"]["url"],
                            Type = ImageType.Primary,
                            ProviderName = PhoenixAdultProvider.PluginName
                        });
                        images.Add(new RemoteImageInfo
                        {
                            Url = (string)image["xx"]["url"],
                            Type = ImageType.Backdrop,
                            ProviderName = PhoenixAdultProvider.PluginName
                        });
                    }

            return images;
        }
    }
}
