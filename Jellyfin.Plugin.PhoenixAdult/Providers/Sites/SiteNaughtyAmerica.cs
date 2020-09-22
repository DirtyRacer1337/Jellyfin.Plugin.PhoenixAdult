using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace PhoenixAdult.Sites
{
    internal class SiteNaughtyAmerica : IPhoenixAdultProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchData, CancellationToken cancellationToken)
        {
            var param = $"{{'requests':[{{'indexName':'nacms_scenes_production','params':'{searchData}&hitsPerPage=100'}}]}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "application/json" }
            };

            var http = await HTTP.POST(url, param, cancellationToken, headers).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            JObject searchResults;
            var searchSceneID = searchTitle.Split()[0];
            string searchParams;
            if (int.TryParse(searchSceneID, out _))
                searchParams = $"filters=id={searchSceneID}";
            else
                searchParams = $"query={searchTitle}";
            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            searchResults = await GetDataFromAPI(url, searchParams, cancellationToken).ConfigureAwait(false);

            foreach (var searchResult in searchResults["results"].First["hits"])
            {
                string sceneID = (string)searchResult["id"],
                        curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}",
                        sceneName = (string)searchResult["title"];
                long sceneDate = (long)searchResult["published_at"];

                var sceneURL = $"https://www.naughtyamerica.com/scene/0{sceneID}";
                var posters = (await GetImages(new Movie
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                }, cancellationToken).ConfigureAwait(false)).Where(item => item.Type == ImageType.Primary);

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    PremiereDate = DateTimeOffset.FromUnixTimeSeconds(sceneDate).DateTime
                };

                if (posters.Any())
                    res.ImageUrl = posters.First().Url;

                result.Add(res);
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
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            var sceneData = await GetDataFromAPI(url, $"filters=id={sceneID[2]}", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["results"].First["hits"].First;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["synopsis"];
            result.Item.AddStudio("Naughty America");

            DateTimeOffset sceneDateObj = DateTimeOffset.FromUnixTimeSeconds((long)sceneData["published_at"]);
            result.Item.PremiereDate = sceneDateObj.DateTime;

            foreach (var genreLink in sceneData["fantasies"])
            {
                var genreName = (string)genreLink;

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["performers"])
            {
                string actorName = (string)actorLink,
                        actorPhoto = string.Empty,
                        actorsPageURL;

                actorsPageURL = actorName.ToLowerInvariant().Replace(" ", "-", StringComparison.OrdinalIgnoreCase).Replace("'", string.Empty, StringComparison.OrdinalIgnoreCase);

                var actorURL = $"https://www.naughtyamerica.com/pornstar/{actorsPageURL}";
                var actorData = await HTML.ElementFromURL(actorURL, cancellationToken).ConfigureAwait(false);

                var actorImageNode = actorData.SelectSingleNode("//img[@class='performer-pic']");
                if (actorImageNode != null)
                    actorPhoto = actorImageNode.Attributes["src"]?.Value;

                var actor = new PersonInfo
                {
                    Name = actorName
                };
                if (!string.IsNullOrEmpty(actorPhoto))
                    actor.ImageUrl = $"https:{actorPhoto}";

                result.People.Add(actor);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            var sceneURL = $"https://www.naughtyamerica.com/scene/0{sceneID[2]}";
            var sceneDataHTML = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var images = sceneDataHTML.SelectNodes("//div[contains(@class, 'contain-scene-images') and contains(@class, 'desktop-only')]/a");
            if (images != null)
                foreach (var sceneImages in sceneDataHTML.SelectNodes("//div[contains(@class, 'contain-scene-images') and contains(@class, 'desktop-only')]/a"))
                {
                    var image = $"https:{sceneImages.Attributes["href"].Value}";
                    result.Add(new RemoteImageInfo
                    {
                        Url = image,
                        Type = ImageType.Primary
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = image,
                        Type = ImageType.Backdrop
                    });
                }

            return result;
        }
    }
}
