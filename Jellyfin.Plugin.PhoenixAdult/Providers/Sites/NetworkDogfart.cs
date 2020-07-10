using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
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
                            subSite = searchResult.SelectSingleNode(".//div/p[@class='help-block']").InnerText.Split(".com")[0];

                    var res = new RemoteSearchResult
                    {
                        Name = $"{sceneName} from {subSite}",
                        ImageUrl = posterURL
                    };

                    if (searchDate.HasValue)
                    {
                        res.PremiereDate = searchDate;
                        curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultHelper.Lang)}";
                    }

                    res.ProviderIds.Add(PhoenixAdultProvider.PluginName, curID);

                    if (subSite == PhoenixAdultHelper.GetSearchSiteName(siteNum))
                        res.IndexNumber = PhoenixAdultHelper.LevenshteinDistance(searchTitle, sceneName) - 100;
                    else
                        res.IndexNumber = PhoenixAdultHelper.LevenshteinDistance(searchTitle, sceneName) - 60;

                    result.Add(res);
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

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//div[@class='icon-container']/a").Attributes["title"].Value;
            result.Item.Overview = sceneData.SelectSingleNode("//div[contains(@class, 'description')]").InnerText.Replace("...read more", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            result.Item.AddStudio("Dogfart Network");

            if (sceneID.Length > 3)
                if (DateTime.TryParseExact(sceneID[3], "yyyy-MM-dd", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                    result.Item.ProductionYear = sceneDateObj.Year;
                }

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

                    result.AddPerson(new PersonInfo
                    {
                        Name = actorName,
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
            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            var poster = sceneData.SelectSingleNode("//div[@class='icon-container']//img");
            if (poster != null)
                images.Add(new RemoteImageInfo
                {
                    Url = $"https:{poster.Attributes["src"].Value}",
                    Type = ImageType.Primary,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

            var img = sceneData.SelectNodes("//div[contains(@class, 'preview-image-container')]//a");
            if (img != null)
                foreach (var sceneImages in img)
                {
                    var url = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + sceneImages.Attributes["href"].Value;
                    var posterPage = await url.GetAsync(cancellationToken).ConfigureAwait(false);
                    var posterHTML = new HtmlDocument();
                    posterHTML.Load(await posterPage.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    var posterData = posterHTML.DocumentNode.SelectSingleNode("//div[contains(@class, 'remove-bs-padding')]/img").Attributes["src"].Value;

                    images.Add(new RemoteImageInfo
                    {
                        Url = posterData,
                        Type = ImageType.Backdrop,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });
                }

            return images;
        }
    }
}
