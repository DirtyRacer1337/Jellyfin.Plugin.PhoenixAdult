using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
{
    public class NetworkBrazzers : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, string searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var http = await PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = PhoenixAdultHelper.GetSearchSearchURL(siteNum),
                RequestHeaders = {
                    { "Cookie", $"textSearch={encodedTitle}" }
                },
                DecompressionMethod = CompressionMethod.None
            }).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(http.Content);

            var searchResults = html.DocumentNode.SelectNodes("//div[@class='release-card-wrap']");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//div[@class='scene-card-info']//a[1]").Attributes["href"].Value,
                        curID = $"{siteNum[0]}${siteNum[1]}${PhoenixAdultHelper.Encode(sceneURL)}",
                        sceneName = searchResult.SelectSingleNode(".//div[@class='scene-card-info']//a[1]").Attributes["title"].Value,
                        scenePoster = $"https:{searchResult.SelectSingleNode(".//img[contains(@class, 'card-main-img')]").Attributes["data-src"].Value}",
                        sceneDate = searchResult.SelectSingleNode(".//time").InnerText.Trim();

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                    SearchProviderName = sceneURL
                };

                if (DateTime.TryParse(sceneDate, out DateTime sceneDateObj))
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

            var http = await PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = PhoenixAdultHelper.Decode(sceneID[2]),
                DecompressionMethod = CompressionMethod.None
            }).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(http.Content);
            var sceneData = html.DocumentNode.SelectSingleNode("//p[@itemprop='description']");

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText;
            result.Item.Overview = sceneData.SelectSingleNode("//p[@itemprop='description']/text()").InnerText.Trim();
            result.Item.AddStudio("Brazzers");

            if (DateTime.TryParse(sceneData.SelectSingleNode("//aside[contains(@class, 'scene-date')]").InnerText, out DateTime sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
                result.Item.ProductionYear = sceneDateObj.Year;
            }

            foreach (var genreLink in sceneData.SelectNodes("//div[contains(@class, 'tag-card-container')]//a"))
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData.SelectNodes("//div[@class='model-card']"))
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

            string[] sceneID = item.ProviderIds[PhoenixAdultProvider.PluginName].Split('$');

            var http = await PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = PhoenixAdultHelper.Decode(sceneID[2]),
                DecompressionMethod = CompressionMethod.None
            }).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(http.Content);
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
