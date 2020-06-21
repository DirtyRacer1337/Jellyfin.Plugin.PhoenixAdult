using System;
using System.Collections.Generic;
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
    internal class SiteBrazzers : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var sceneID = searchTitle.Split()[0];
            if (int.TryParse(sceneID, out _))
            {
                string sceneURL = $"{PhoenixAdultHelper.GetSearchBaseURL(siteNum)}/scenes/view/id/{sceneID}",
                       curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);

                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                    Name = sceneData.Item.Name
                });
            }
            else
            {
                var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum);
                var http = await url.WithHeader("Cookie", $"textSearch={encodedTitle}").GetAsync(cancellationToken).ConfigureAwait(false);
                var html = new HtmlDocument();
                html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

                var searchResults = html.DocumentNode.SelectNodes("//div[@class='release-card-wrap']");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//div[@class='scene-card-info']//a[1]").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div[@class='scene-card-info']//a[1]").Attributes["title"].Value,
                            scenePoster = $"https:{searchResult.SelectSingleNode(".//img[contains(@class, 'card-main-img')]").Attributes["data-src"].Value}",
                            sceneDate = searchResult.SelectSingleNode(".//time").InnerText.Trim();

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster
                    };

                    if (DateTime.TryParse(sceneDate, out DateTime sceneDateObj))
                        res.PremiereDate = sceneDateObj;

                    result.Add(res);
                }
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

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode.SelectSingleNode("//p[@itemprop='description']");

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText;
            result.Item.Overview = sceneData.SelectSingleNode("//p[@itemprop='description']/text()").InnerText.Trim();
            result.Item.AddStudio("Brazzers");

            var dateNode = sceneData.SelectSingleNode("//aside[contains(@class, 'scene-date')]");
            if (dateNode != null)
                if (DateTime.TryParse(dateNode.InnerText, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                    result.Item.ProductionYear = sceneDateObj.Year;
                }

            var genreNode = sceneData.SelectNodes("//div[contains(@class, 'tag-card-container')]//a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Item.AddGenre(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@class='model-card']");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.SelectSingleNode(".//h2[@class='model-card-title']//a").Attributes["title"].Value,
                           actorPhoto = $"https:{actorLink.SelectSingleNode(".//div[@class='card-image']//img").Attributes["data-src"].Value}";

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

            foreach (var sceneImages in sceneData.SelectNodes("//*[@id='trailer-player']/img"))
                images.Add(new RemoteImageInfo
                {
                    Url = $"https:{sceneImages.Attributes["src"].Value}",
                    Type = ImageType.Primary,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

            foreach (var sceneImages in sceneData.SelectNodes("//a[@rel='preview']"))
                images.Add(new RemoteImageInfo
                {
                    Url = $"https:{sceneImages.Attributes["href"].Value}",
                    Type = ImageType.Backdrop,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

            return images;
        }
    }
}
