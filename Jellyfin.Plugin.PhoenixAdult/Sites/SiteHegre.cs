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
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class SiteHegre : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            if (searchDate.HasValue)
            {
                searchTitle += $"&year={searchDate.Value.Year}";
            }

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[contains(@class, 'item')]");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//a/@href"));
                if (sceneURL.AbsolutePath.Contains("/films/", StringComparison.OrdinalIgnoreCase) || sceneURL.AbsolutePath.Contains("/massage/", StringComparison.OrdinalIgnoreCase))
                {
                    string curID = Helper.Encode(sceneURL.AbsolutePath),
                        sceneName = searchResult.SelectSingleText(".//img/@alt"),
                        scenePoster = searchResult.SelectSingleText(".//img/@data-src"),
                        sceneDate = searchResult.SelectSingleText(".//div[@class='details']/span[last()]");

                    var res = new RemoteSearchResult
                    {
                        Name = sceneName,
                        ImageUrl = scenePoster,
                    };

                    if (!string.IsNullOrEmpty(sceneDate))
                    {
                        sceneDate = sceneDate
                            .Replace("nd", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("th", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("rd", string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("st", string.Empty, StringComparison.OrdinalIgnoreCase);
                        if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                        {
                            res.PremiereDate = sceneDateObj;
                        }
                    }

                    res.ProviderIds.Add(Plugin.Instance.Name, curID);

                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//div[@class='title']/h1");

            var description = sceneData.SelectSingleText("//div[contains(@class, 'record-description-content')]");
            description = description.Substring(0, description.IndexOf("Runtime", StringComparison.OrdinalIgnoreCase));
            result.Item.Overview = description;

            result.Item.AddStudio("Hegre");

            var date = sceneData.SelectSingleText("//span[@class='date']");
            if (DateTime.TryParseExact(date, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodesSafe("//a[@class='tag']");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//a[@class='record-model']");
            foreach (var actorLink in actorsNode)
            {
                var actor = new PersonInfo
                {
                    Name = actorLink.Attributes["title"].Value,
                    ImageUrl = actorLink.SelectSingleText(".//img/@src").Replace("150x", "480x", StringComparison.OrdinalIgnoreCase).Replace("240x", "480x", StringComparison.OrdinalIgnoreCase),
                };

                result.People.Add(actor);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var img = sceneData.SelectSingleText("//meta[@name='twitter:image']/@content");

            result.Add(new RemoteImageInfo
            {
                Url = img
                .Replace("board-image", "poster-image", StringComparison.OrdinalIgnoreCase)
                .Replace("1600x", "640x", StringComparison.OrdinalIgnoreCase),
                Type = ImageType.Primary,
            });
            result.Add(new RemoteImageInfo
            {
                Url = img.Replace("1600x", "1920x", StringComparison.OrdinalIgnoreCase),
                Type = ImageType.Backdrop,
            });

            return result;
        }
    }
}
