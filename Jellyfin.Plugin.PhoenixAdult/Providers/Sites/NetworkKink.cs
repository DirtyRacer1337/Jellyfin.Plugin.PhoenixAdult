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
    internal class NetworkKink : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var sceneID = searchTitle.Split()[0];
            if (int.TryParse(sceneID, out _))
            {
                string sceneURL = $"{PhoenixAdultHelper.GetSearchBaseURL(siteNum)}/shoot/{sceneID}",
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
                var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + encodedTitle;
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodes("//div[@class='shoot-card scene']");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a[@class='shoot-link']").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//img").Attributes["alt"].Value.Trim(),
                            scenePoster = searchResult.SelectSingleNode(".//img").Attributes["src"].Value,
                            sceneDate = searchResult.SelectSingleNode(".//div[@class='date']").InnerText.Trim();

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

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.WithCookie("viewing-preferences", "straight%2Cgay").GetAsync(cancellationToken).ConfigureAwait(false);
            var stream = await http.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var sceneData = HTML.ElementFromStream(stream);

            result.Item.Name = sceneData.SelectSingleNode("//h1[@class='shoot-title']").GetDirectInnerText().Trim();
            result.Item.Overview = sceneData.SelectNodes("//div[@class='description']")[1].InnerText.Replace("Description:", "", StringComparison.OrdinalIgnoreCase).Trim();
            result.Item.AddStudio("Kink");

            var sceneDate = sceneData.SelectSingleNode("//span[@class='shoot-date']").InnerText.Trim();
            if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
                result.Item.ProductionYear = sceneDateObj.Year;
            }

            foreach (var genreLink in sceneData.SelectNodes("//p[@class='tag-list category-tag-list']//a"))
            {
                var genreName = genreLink.InnerText.Replace(",", "", StringComparison.OrdinalIgnoreCase).Trim();

                result.Item.AddGenre(genreName);
            }

            var actors = sceneData.SelectNodes("//p[@class='starring']//a");
            if (actors != null)
                foreach (var actorLink in actors)
                {
                    string actorName = actorLink.InnerText.Replace(",", "", StringComparison.OrdinalIgnoreCase).Trim(),
                           actorPageURL = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                           actorPhoto;

                    http = await actorPageURL.GetAsync(cancellationToken).ConfigureAwait(false);
                    var actorHTML = new HtmlDocument();
                    actorHTML.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    actorPhoto = actorHTML.DocumentNode.SelectSingleNode("//div[contains(@class, 'biography-container')]//img").Attributes["src"].Value;

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
            var http = await sceneURL.WithCookie("viewing-preferences", "straight%2Cgay").GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            var sceneImages = sceneData.SelectNodes("//video");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                    images.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["poster"].Value,
                        Type = ImageType.Primary,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });

            sceneImages = sceneData.SelectNodes("//div[@class='player']//img");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                {
                    images.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["src"].Value,
                        Type = ImageType.Primary,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });
                    images.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["src"].Value,
                        Type = ImageType.Backdrop,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });
                }

            sceneImages = sceneData.SelectNodes("//div[@id='previewImages']//img");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                {
                    images.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["data-image-file"].Value,
                        Type = ImageType.Primary,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });
                    images.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["data-image-file"].Value,
                        Type = ImageType.Backdrop,
                        ProviderName = PhoenixAdultProvider.PluginName
                    });
                }

            return images;
        }
    }
}
