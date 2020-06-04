using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
{
    class SiteNaughtyAmerica : IPhoenixAdultProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchData, CancellationToken cancellationToken)
        {
            var param = $"{{'requests':[{{'indexName':'nacms_scenes_production','params':'{searchData}&hitsPerPage=100'}}]}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "application/json" }
            };

            var http = await url.WithHeaders(headers).PostStringAsync(param, cancellationToken).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, string searchDate, CancellationToken cancellationToken)
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

                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                    Name = sceneName,
                    PremiereDate = DateTimeOffset.FromUnixTimeSeconds(sceneDate).DateTime
                });
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

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            var sceneData = await GetDataFromAPI(url, $"filters=id={sceneID[2]}", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["results"].First["hits"].First;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["synopsis"];
            result.Item.AddStudio("Naughty America");

            DateTimeOffset sceneDateObj = DateTimeOffset.FromUnixTimeSeconds((long)sceneData["published_at"]);
            result.Item.PremiereDate = sceneDateObj.DateTime;
            result.Item.ProductionYear = sceneDateObj.Year;

            foreach (var genreLink in sceneData["fantasies"])
            {
                if (genreLink.HasValues && (genreLink["name"] != null))
                    result.Item.AddGenre((string)genreLink["name"]);
            }

            foreach (var actorLink in sceneData["performers"])
            {
                if (actorLink.HasValues && (actorLink["name"] != null))
                {
                    string actorName = (string)actorLink["name"],
                           actorPhoto = string.Empty,
                           actorsPageURL;

                    if (actorLink["slug"] != null)
                        actorsPageURL = (string)actorLink["slug"];
                    else
                    {
                        actorsPageURL = (string)actorLink["name"];
                        actorsPageURL = actorsPageURL.ToLower(PhoenixAdultHelper.Lang).Replace(" ", "-", StringComparison.OrdinalIgnoreCase).Replace("'", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    var http = await $"https://www.naughtyamerica.com/pornstar/{actorsPageURL}".GetAsync(cancellationToken).ConfigureAwait(false);
                    var html = new HtmlDocument();
                    html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var actorData = html.DocumentNode;

                    var actorImageNode = actorData.SelectSingleNode("//img[@class='performer-pic']");
                    if (actorImageNode != null)
                        actorPhoto = actorImageNode.Attributes["src"]?.Value;

                    result.AddPerson(new PersonInfo
                    {
                        Name = actorName,
                        Type = PersonType.Actor
                    });
                    if (!string.IsNullOrEmpty(actorPhoto))
                        result.People.Last().ImageUrl = $"https:{actorPhoto}";
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

            var http = await $"https://www.naughtyamerica.com/scene/0{sceneID[2]}".GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            foreach (var sceneImages in sceneData.SelectNodes("//div[contains(@class, 'contain-scene-images') and contains(@class, 'desktop-only')]/a"))
            {
                var image = $"https:{sceneImages.Attributes["href"].Value}";
                images.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Primary,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

                images.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Backdrop,
                    ProviderName = PhoenixAdultProvider.PluginName
                });
            }

            return images;
        }
    }
}
