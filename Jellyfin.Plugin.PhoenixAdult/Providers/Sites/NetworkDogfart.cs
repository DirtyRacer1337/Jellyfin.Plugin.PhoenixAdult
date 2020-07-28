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
using PhoenixAdult.Providers.Helpers;

namespace PhoenixAdult.Providers.Sites
{
    internal class NetworkDogfart : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//a[contains(@class, 'thumbnail')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + searchResult.Attributes["href"].Value.Split('?')[0],
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div/h3[@class='scene-title']").InnerText,
                            posterURL = $"https:{searchResult.SelectSingleNode(".//img").Attributes["src"].Value}",
                            subSite = searchResult.SelectSingleNode(".//div/p[@class='help-block']").InnerText.Replace(".com", "", StringComparison.OrdinalIgnoreCase);

                    var res = new RemoteSearchResult
                    {
                        Name = $"{sceneName} from {subSite}",
                        ImageUrl = posterURL
                    };

                    if (searchDate.HasValue)
                        curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultProvider.Lang)}";

                    res.ProviderIds.Add(Plugin.Instance.Name, curID);

                    if (subSite == PhoenixAdultHelper.GetSearchSiteName(siteNum))
                        res.IndexNumber = 100 - LevenshteinDistance.Calculate(searchTitle, sceneName);
                    else
                        res.IndexNumber = 60 - LevenshteinDistance.Calculate(searchTitle, sceneName);

                    Console.WriteLine(res.IndexNumber);

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
                return result;

            string sceneURL = PhoenixAdultHelper.Decode(sceneID[2]),
                sceneDate = string.Empty;

            if (sceneID.Length > 3)
                sceneDate = sceneID[3];

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//div[@class='icon-container']/a").Attributes["title"].Value;
            result.Item.Overview = sceneData.SelectSingleNode("//div[contains(@class, 'description')]").InnerText.Replace("...read more", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            result.Item.AddStudio("Dogfart Network");

            if (!string.IsNullOrEmpty(sceneDate))
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", PhoenixAdultProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.Item.PremiereDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("//div[@class='categories']/p/a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim();

                    result.Item.AddGenre(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//h4[@class='more-scenes']/a");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

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

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            string sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var poster = sceneData.SelectSingleNode("//div[@class='icon-container']//img");
            if (poster != null)
                result.Add(new RemoteImageInfo
                {
                    Url = $"https:{poster.Attributes["src"].Value}",
                    Type = ImageType.Primary
                });

            var img = sceneData.SelectNodes("//div[contains(@class, 'preview-image-container')]//a");
            if (img != null)
                foreach (var sceneImages in img)
                {
                    var url = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + sceneImages.Attributes["href"].Value;
                    var posterHTML = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                    var posterData = posterHTML.SelectSingleNode("//div[contains(@class, 'remove-bs-padding')]/img").Attributes["src"].Value;
                    result.Add(new RemoteImageInfo
                    {
                        Url = posterData,
                        Type = ImageType.Backdrop
                    });
                }

            return result;
        }
    }
}
