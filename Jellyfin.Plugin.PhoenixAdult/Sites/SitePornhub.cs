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
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    internal class SitePornhub : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;
            if ((searchTitle.StartsWith("ph", StringComparison.OrdinalIgnoreCase) || int.TryParse(searchTitle, out _)) && !searchTitle.Contains(" ", StringComparison.OrdinalIgnoreCase))
            {
                string sceneURL = $"{Helper.GetSearchBaseURL(siteNum)}/view_video.php?viewkey={searchTitle}",
                       curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);
                sceneData.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                var posters = (await GetImages(sceneData.Item, cancellationToken).ConfigureAwait(false)).Where(item => item.Type == ImageType.Primary);

                var res = new RemoteSearchResult
                {
                    ProviderIds = sceneData.Item.ProviderIds,
                    Name = sceneData.Item.Name,
                    PremiereDate = sceneData.Item.PremiereDate
                };

                if (posters.Any())
                    res.ImageUrl = posters.First().Url;

                result.Add(res);
            }
            else
            {
                searchTitle = searchTitle.Replace(" ", "+", StringComparison.OrdinalIgnoreCase);
                var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodes("//ul[@id='videoSearchResult']/li[@_vkey]");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//span[@class='title']").InnerText.Trim(),
                            scenePoster = searchResult.SelectSingleNode(".//div[@class='phimage']//img").Attributes["data-thumb_url"].Value;

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster
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

            var sceneURL = Helper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);
            var sceneDataJSON = JObject.Parse(sceneData.SelectSingleNode("//script[@type='application/ld+json']").InnerText.Trim());

            result.Item.Name = sceneData.SelectSingleNode("//h1[@class='title']").InnerText.Trim();
            var studioName = sceneData.SelectSingleNode("//div[@class='userInfo']//a").InnerText.Trim();
            result.Item.AddStudio(studioName);

            var date = (string)sceneDataJSON["uploadDate"];
            if (date != null)
                if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.Item.PremiereDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("(//div[@class='categoriesWrapper'] | //div[@class='tagsWrapper'])/a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim();

                    result.Item.AddGenre(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@class='pornstarsWrapper']/a");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.Attributes["data-mxptext"].Value,
                           actorPhotoURL = actorLink.SelectSingleNode(".//img[@class='avatar']").Attributes["src"].Value;
                    result.People.Add(new PersonInfo
                    {
                        Name = actorName,
                        ImageUrl = actorPhotoURL
                    });
                }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            var sceneURL = Helper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var imgNode = sceneData.SelectSingleNode("//div[@id='player']//img");
            if (imgNode != null)
                result.Add(new RemoteImageInfo
                {
                    Url = imgNode.Attributes["src"].Value,
                    Type = ImageType.Primary
                });

            return result;
        }
    }
}
