using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Providers.Helpers;

namespace PhoenixAdult.Providers.Sites
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
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster
                    };
                    if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", PhoenixAdultProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
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
                Item = new Movie(),
                People = new List<PersonInfo>()
            };

            if (sceneID == null)
                return result;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.WithCookie("viewing-preferences", "straight%2Cgay").GetAsync(cancellationToken).ConfigureAwait(false);
            var stream = await http.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var sceneData = HTML.ElementFromStream(stream);

            result.Item.Name = sceneData.SelectSingleNode("//h1[@class='shoot-title']").InnerText.Trim();
            result.Item.Overview = sceneData.SelectSingleNode("//p[@class='description-text']").InnerText.Trim();
            result.Item.AddStudio("Kink");

            var sceneDate = sceneData.SelectSingleNode("//span[@class='shoot-date']").InnerText.Trim();
            if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", PhoenixAdultProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                result.Item.PremiereDate = sceneDateObj;

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

                    result.People.Add(new PersonInfo
                    {
                        Name = actorName,
                        ImageUrl = actorPhoto
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

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.WithCookie("viewing-preferences", "straight%2Cgay").GetAsync(cancellationToken).ConfigureAwait(false);
            var stream = await http.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var sceneData = HTML.ElementFromStream(stream);

            var sceneImages = sceneData.SelectNodes("//video");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["poster"].Value,
                        Type = ImageType.Primary
                    });

            sceneImages = sceneData.SelectNodes("//div[@class='player']//img");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["src"].Value,
                        Type = ImageType.Primary
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["src"].Value,
                        Type = ImageType.Backdrop
                    });
                }

            sceneImages = sceneData.SelectNodes("//div[@id='previewImages']//img");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["data-image-file"].Value,
                        Type = ImageType.Primary
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["data-image-file"].Value,
                        Type = ImageType.Backdrop
                    });
                }

            return result;
        }
    }
}
