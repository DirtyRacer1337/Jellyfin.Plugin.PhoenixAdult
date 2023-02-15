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
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class NetworkStrike3 : IProviderBase
    {
        private readonly string searchVariables = "{{\"query\":\"{0}\",\"site\":\"{1}\",\"first\":10,\"skip\":0}}";
        private readonly string searchQuery = @"query getSearchResults($query:String!,$site:Site!,$first:Int,$skip:Int){searchVideos(input:{query:$query,site:$site,first:$first,skip:$skip}){edges{node{videoId title releaseDate slug images{listing{src}}}}}}";
        private readonly string updateVariables = "{{\"slug\":\"{0}\",\"site\":\"{1}\"}}";
        private readonly string updateQuery = @"query getSearchResults($slug:String!,$site:Site!){findOneVideo(input:{slug:$slug,site:$site}){videoId title description releaseDate models{name slug images{listing{highdpi{double}}}}directors{name}categories{name}carousel{listing{highdpi{triple}}}}}";

        public static async Task<JObject> GetDataFromAPI(string url, string query, string variables, CancellationToken cancellationToken)
        {
            JObject json = null;

            var param = new StringContent($"{{\"query\":\"{query}\",\"variables\":{variables}}}", Encoding.UTF8, "application/json");
            var http = await HTTP.Request(url, HttpMethod.Post, param, cancellationToken).ConfigureAwait(false);

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

            var variables = string.Format(this.searchVariables, searchTitle, Helper.GetSearchSiteName(siteNum).ToUpper());
            var url = Helper.GetSearchSearchURL(siteNum);
            var searchResults = await GetDataFromAPI(url, this.searchQuery, variables, cancellationToken).ConfigureAwait(false);
            if (searchResults == null)
            {
                return result;
            }

            foreach (var searchResult in searchResults["searchVideos"]["edges"])
            {
                string sceneURL = (string)searchResult["node"]["slug"],
                        curID = Helper.Encode(sceneURL),
                        sceneName = (string)searchResult["node"]["title"],
                        scenePoster = (string)searchResult["node"]["images"]["listing"].First()["src"];
                var sceneDateObj = (DateTime)searchResult["node"]["releaseDate"];

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

            var sceneURL = Helper.Decode(sceneID[0]);

            var variables = string.Format(this.updateVariables, sceneURL, Helper.GetSearchSiteName(siteNum).ToUpper());
            var url = Helper.GetSearchSearchURL(siteNum);
            var sceneData = await GetDataFromAPI(url, this.updateQuery, variables, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["findOneVideo"];

            result.Item.ExternalId = Helper.GetSearchBaseURL(siteNum) + $"/videos/{sceneURL}";

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            result.Item.AddStudio(Helper.GetSearchSiteName(siteNum));

            var sceneDateObj = (DateTime)sceneData["releaseDate"];
            result.Item.PremiereDate = sceneDateObj;

            foreach (var genreLink in sceneData["categories"])
            {
                string genreName = (string)genreLink["name"];

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["models"])
            {
                var actor = new PersonInfo
                {
                    Name = (string)actorLink["name"],
                };

                if (actorLink["images"].Any())
                {
                    actor.ImageUrl = (string)actorLink["images"]["listing"].First()["highdpi"]["double"];
                }

                result.People.Add(actor);
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

            var variables = string.Format(this.updateVariables, sceneURL, Helper.GetSearchSiteName(siteNum).ToUpper());
            var url = Helper.GetSearchSearchURL(siteNum);
            var sceneData = await GetDataFromAPI(url, this.updateQuery, variables, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            var video = (JObject)sceneData["findOneVideo"];

            foreach (var image in video["carousel"])
            {
                var img = (string)image["listing"].First()["highdpi"]["triple"];

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
