using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
{
    internal class NetworkR18 : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            string searchJAVID = string.Empty;
            var sceneID = searchTitle.Split();
            if (int.TryParse(sceneID[1], out _))
                searchJAVID = $"{sceneID[0]}%20{sceneID[1]}";

            if (!string.IsNullOrEmpty(searchJAVID))
                encodedTitle = searchJAVID;

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//li[contains(@class, 'item-list')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID,
                            sceneName = searchResult.SelectSingleNode(".//dt").InnerText,
                            scenePoster = searchResult.SelectSingleNode(".//img").Attributes["data-original"].Value,
                            javID = searchResult.SelectSingleNode(".//img").Attributes["alt"].Value;

                    sceneURL = sceneURL.Replace("/" + sceneURL.Split('/').Last(), string.Empty, StringComparison.OrdinalIgnoreCase);
                    curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}";

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster
                    };

                    if (!string.IsNullOrEmpty(searchJAVID))
                        res.IndexNumber = PhoenixAdultHelper.LevenshteinDistance(searchJAVID, javID);

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

            result.Item.Name = sceneData.SelectSingleNode("//cite[@itemprop='name']").InnerText;

            var description = sceneData.SelectSingleNode("//div[@class='cmn-box-description01']");
            if (description != null)
                result.Item.Overview = description.InnerText.Replace("Product Description", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            result.Item.AddStudio(sceneData.SelectSingleNode("//dd[@itemprop='productionCompany']").InnerText.Trim());

            var dateNode = sceneData.SelectSingleNode("//dd[@itemprop='dateCreated']");
            if (dateNode != null)
            {
                var date = dateNode.InnerText.Trim().Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase).Replace("June", "Jun", StringComparison.OrdinalIgnoreCase).Replace("July", "Jul", StringComparison.OrdinalIgnoreCase);
                if (DateTime.TryParseExact(date, "MMM dd yyyy", PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                    result.Item.ProductionYear = sceneDateObj.Year;
                }
            }

            var genreNode = sceneData.SelectNodes("//a[@itemprop='genre']");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim().ToLower(PhoenixAdultHelper.Lang);

                    result.Item.AddGenre(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@itemprop='actors']//span[@itemprop='name']");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

                    if (actorName != "----")
                    {
                        actorName = actorName.Split('(')[0].Trim();

                        var actor = new PersonInfo
                        {
                            Name = actorName,
                            Type = PersonType.Actor
                        };

                        var photoXpath = string.Format(PhoenixAdultHelper.Lang, "//div[@id='{0}']//img[contains(@alt, '{1}')]", actorName.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase), actorName);
                        var actorPhoto = sceneData.SelectSingleNode(photoXpath).Attributes["src"].Value;

                        if (!actorPhoto.Contains("nowprinting.gif", StringComparison.OrdinalIgnoreCase))
                            actor.ImageUrl = actorPhoto;

                        result.AddPerson(actor);
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

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            images.Add(new RemoteImageInfo
            {
                Url = sceneData.SelectSingleNode("//img[contains(@alt, 'cover')]").Attributes["src"].Value,
                Type = ImageType.Primary,
                ProviderName = PhoenixAdultProvider.PluginName
            });

            foreach (var sceneImages in sceneData.SelectNodes("//section[@id='product-gallery']//img"))
                images.Add(new RemoteImageInfo
                {
                    Url = sceneImages.Attributes["data-src"].Value,
                    Type = ImageType.Backdrop,
                    ProviderName = PhoenixAdultProvider.PluginName
                });

            return images;
        }
    }
}
