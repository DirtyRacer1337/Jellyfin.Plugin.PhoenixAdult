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
    internal class NetworkFemdomEmpire : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//div[contains(@class, 'item-info')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//a").InnerText.Trim(),
                            sceneDate = searchResult.SelectSingleNode(".//span[@class='date']").InnerText.Trim();

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                        Name = sceneName
                    };

                    if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                        res.PremiereDate = sceneDateObj;

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

            result.Item.Name = sceneData.SelectSingleNode("//div[contains(@class, 'videoDetails')]//h3").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@class, 'videoDetails')]//p");
            if (description != null)
                result.Item.Overview = description.InnerText.Trim();
            result.Item.AddStudio("Femdom Empire");

            var dateNode = sceneData.SelectSingleNode("//div[contains(@class, 'videoInfo')]//p");
            if (dateNode != null)
            {
                var date = dateNode.InnerText.Replace("Date Added:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                if (DateTime.TryParseExact(date, "MMMM d, yyyy", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                    result.Item.ProductionYear = sceneDateObj.Year;
                }
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

            var img = sceneData.SelectSingleNode("//a[@class='fake_trailer']//img");
            if (img != null)
            {
                images.Add(new RemoteImageInfo
                {
                    Url = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + img.Attributes["src0_1x"].Value,
                    Type = ImageType.Primary,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

                images.Add(new RemoteImageInfo
                {
                    Url = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + img.Attributes["src0_1x"].Value,
                    Type = ImageType.Backdrop,
                    ProviderName = PhoenixAdultProvider.PluginName
                });
            }

            return images;
        }
    }
}
