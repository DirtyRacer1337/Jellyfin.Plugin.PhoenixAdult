using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
{
    public class NetworkBang : IPhoenixAdultProviderBase
    {
        public async Task<JObject> GetDataFromAPI(string url, string searchTitle, string searchType, CancellationToken cancellationToken)
        {
            var param = $"{{'query':{{'bool':{{'must':[{{'match':{{'{searchType}':'{searchTitle}'}}}},{{'match':{{'type':'movie'}}}}],'must_not':[{{'match':{{'type':'trailer'}}}}]}}}}}}".Replace('\'', '"');
            var http = await PhoenixAdultProvider.Http.Post(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                RequestHeaders = {
                    { "Authorization", "Basic YmFuZy1yZWFkOktqVDN0RzJacmQ1TFNRazI=" }
                },
                RequestContentType = "application/json",
                DecompressionMethod = CompressionMethod.None,
                RequestContent = param
            }).ConfigureAwait(false);

            var data = new StreamReader(http.Content);
            var json = JObject.Parse(await data.ReadToEndAsync().ConfigureAwait(false));
            data.Dispose();

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, string searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var searchResults = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), searchTitle, "name", cancellationToken).ConfigureAwait(false);
            foreach (var searchResult in searchResults["hits"]["hits"])
            {
                var sceneData = searchResult["_source"];
                string sceneID = (string)sceneData["identifier"],
                        curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}",
                        sceneName = (string)sceneData["name"],
                        scenePoster = $"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg",
                        sceneDescription = (string)sceneData["description"],
                        sceneDate = (string)sceneData["releaseDate"];

                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                    Overview = sceneDescription
                });
                if (DateTime.TryParse(sceneDate, out DateTime sceneDateObj))
                    result.Last().PremiereDate = sceneDateObj;
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

            var sceneData = await GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), sceneID[2], "identifier", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["hits"]["hits"].First["_source"];

            result.Item.Name = (string)sceneData["name"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio(PhoenixAdultHelper.Lang.TextInfo.ToTitleCase((string)sceneData["studio"]["name"]));

            if (DateTime.TryParse((string)sceneData["releaseDate"], out DateTime sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
                result.Item.ProductionYear = sceneDateObj.Year;
            }

            foreach (var genreLink in sceneData["genres"])
            {
                var genreName = (string)genreLink["name"];

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                string actorName = (string)actorLink["name"],
                       actorPhoto = $"https://i.bang.com/pornstars/{actorLink["id"]}.jpg";

                result.AddPerson(new PersonInfo
                {
                    Name = actorName,
                    ImageUrl = actorPhoto,
                    Type = PersonType.Actor
                });
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

            var sceneData = await new NetworkBang().GetDataFromAPI(PhoenixAdultHelper.GetSearchSearchURL(siteNum), sceneID[2], "identifier", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["hits"]["hits"].First["_source"];

            images.Add(new RemoteImageInfo
            {
                Url = $"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg",
                Type = ImageType.Primary,
                ProviderName = PhoenixAdultProvider.PluginName
            });

            foreach (var image in sceneData["screenshots"])
                images.Add(new RemoteImageInfo
                {
                    Url = $"https://i.bang.com/screenshots/{sceneData["dvd"]["id"]}/movie/1/{image["screenId"]}.jpg",
                    Type = ImageType.Backdrop,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

            return images;
        }
    }
}
