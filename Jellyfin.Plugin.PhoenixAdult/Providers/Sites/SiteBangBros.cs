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
    internal class SiteBangBros : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase);
            var http = await url.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

            var searchResults = html.DocumentNode.SelectNodes("//div[contains(@class, 'elipsTxt')]//div[@class='echThumb']");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a[contains(@href, '/video')]").Attributes["href"].Value,
                        curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                        sceneName = searchResult.SelectSingleNode(".//span[@class='thmb_ttl']").InnerText.Trim(),
                        scenePoster = $"https:{searchResult.SelectSingleNode(".//img").Attributes["data-src"].Value}",
                        sceneDate = searchResult.SelectSingleNode(".//span[contains(@class, 'thmb_mr_2')]").InnerText.Trim();

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster
                };

                if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
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
                return result;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText;
            result.Item.Overview = sceneData.SelectSingleNode("//div[@class='vdoDesc']").InnerText.Trim();
            result.Item.AddStudio("Bang Bros");

            var dateNode = sceneData.SelectSingleNode("//span[contains(@class, 'thmb_mr_2')]");
            if (dateNode != null)
                if (DateTime.TryParseExact(dateNode.InnerText.Trim(), "MMM d, yyyy", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                    result.Item.ProductionYear = sceneDateObj.Year;
                }

            var genreNode = sceneData.SelectNodes("//div[contains(@class, 'vdoTags')]//a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim();

                    result.Item.AddGenre(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@class='vdoCast']//a[contains(@href, '/model')]");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim(),
                           actorPageURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                           actorPhoto;

                    http = await actorPageURL.GetAsync(cancellationToken).ConfigureAwait(false);
                    var actorHTML = new HtmlDocument();
                    actorHTML.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    actorPhoto = $"https:{actorHTML.DocumentNode.SelectSingleNode("//div[@class='profilePic_in']//img").Attributes["src"].Value}";

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

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            var imgNode = sceneData.SelectNodes("//img[contains(@id, 'player-overlay-image')]");
            if (imgNode != null)
                foreach (var sceneImages in imgNode)
                    images.Add(new RemoteImageInfo
                    {
                        Url = $"https:{sceneImages.Attributes["src"].Value}",
                        Type = ImageType.Primary,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });

            imgNode = sceneData.SelectNodes("//div[@id='img-slider']//img");
            if (imgNode != null)
                foreach (var sceneImages in imgNode)
                    images.Add(new RemoteImageInfo
                    {
                        Url = $"https:{sceneImages.Attributes["src"].Value}",
                        Type = ImageType.Backdrop,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });

            return images;
        }
    }
}
