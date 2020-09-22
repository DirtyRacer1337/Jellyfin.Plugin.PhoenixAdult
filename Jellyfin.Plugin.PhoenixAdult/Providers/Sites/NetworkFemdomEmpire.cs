using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;

namespace PhoenixAdult.Sites
{
    internal class NetworkFemdomEmpire : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//div[contains(@class, 'item') and contains(@class, 'hover')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div[contains(@class, 'item-info')]//a").InnerText.Trim(),
                            sceneDate = searchResult.SelectSingleNode(".//span[@class='date']").InnerText.Trim(),
                            scenePoster = string.Empty;

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName
                    };

                    var scenePosterNode = searchResult.SelectSingleNode(".//img");
                    if (scenePosterNode.Attributes.Contains("src0_1x"))
                        scenePoster = scenePosterNode.Attributes["src0_1x"].Value;
                    else
                    {
                        if (scenePosterNode.Attributes.Contains("src"))
                            scenePoster = scenePosterNode.Attributes["src"].Value;
                    }

                    if (!string.IsNullOrEmpty(scenePoster))
                    {
                        if (!scenePoster.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            scenePoster = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + scenePoster;
                        res.ImageUrl = scenePoster;
                    }

                    if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                        res.PremiereDate = sceneDateObj;

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

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//div[contains(@class, 'videoDetails')]//h3").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@class, 'videoDetails')]//p");
            if (description != null)
                result.Item.Overview = description.InnerText.Trim();
            result.Item.AddStudio("Femdom Empire");

            var dateNode = sceneData.SelectSingleNode("//div[contains(@class, 'videoInfo')]//p");
            if (dateNode != null)
            {
                var date = dateNode.InnerText.Replace("Date Added:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                if (DateTime.TryParseExact(date, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.Item.PremiereDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodes("//div[contains(@class, 'featuring')][2]//ul//li");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Replace("categories:", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("tags:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                    if (!string.IsNullOrEmpty(genreName))
                        result.Item.AddGenre(genreName);
                }
            result.Item.AddGenre("Femdom");

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'featuring')][1]/ul/li");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Replace("Featuring:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                    if (!string.IsNullOrEmpty(actorName))
                        result.People.Add(new PersonInfo
                        {
                            Name = actorName
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

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            string sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var img = sceneData.SelectSingleNode("//a[@class='fake_trailer']//img");
            if (img != null)
            {
                var image = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + img.Attributes["src0_1x"].Value;
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
