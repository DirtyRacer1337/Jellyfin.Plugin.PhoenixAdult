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

            var searchResults = data.SelectNodes("//div[contains(@class, 'item')]");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value;

                if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
                }

                if (sceneURL.Contains("/films/", StringComparison.OrdinalIgnoreCase) || sceneURL.Contains("/massage/", StringComparison.OrdinalIgnoreCase))
                {
                    string curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                        sceneName = searchResult.SelectSingleNode(".//img").Attributes["alt"].Value,
                        scenePoster = searchResult.SelectSingleNode(".//img").Attributes["data-src"].Value,
                        sceneDate = searchResult.SelectSingleNode(".//div[@class='details']/span[last()]").InnerText.Trim();

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
                        if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
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

        public async Task<MetadataResult<Movie>> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            string sceneURL = Helper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//div[@class='title']/h1").InnerText.Trim();

            var description = sceneData.SelectSingleNode("//div[contains(@class, 'record-description-content')]").InnerText.Trim();
            description = description.Substring(0, description.IndexOf("Runtime", StringComparison.OrdinalIgnoreCase));
            result.Item.Overview = description;

            result.Item.AddStudio("Hegre");

            var dateNode = sceneData.SelectSingleNode("//span[@class='date']");
            if (dateNode != null)
            {
                var date = dateNode.InnerText.Trim();
                if (DateTime.TryParseExact(date, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodes("//a[@class='tag']");
            if (genreNode != null)
            {
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim();

                    result.Item.AddGenre(genreName);
                }
            }

            var actorsNode = sceneData.SelectNodes("//a[@class='record-model']");
            if (actorsNode != null)
            {
                foreach (var actorLink in actorsNode)
                {
                    var actor = new PersonInfo
                    {
                        Name = actorLink.Attributes["title"].Value,
                        ImageUrl = actorLink.SelectSingleNode(".//img").Attributes["src"].Value.Replace("150x", "480x", StringComparison.OrdinalIgnoreCase).Replace("240x", "480x", StringComparison.OrdinalIgnoreCase),
                    };

                    result.People.Add(actor);
                }
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (item == null)
            {
                return result;
            }

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
            {
                return result;
            }

            var sceneID = externalId.Split('#');

            var sceneURL = Helper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var img = sceneData.SelectSingleNode("//meta[@name='twitter:image']").Attributes["content"].Value;

            result.Add(new RemoteImageInfo
            {
                Url = img.Replace("board-image", "poster-image", StringComparison.OrdinalIgnoreCase).Replace("1600x", "640x", StringComparison.OrdinalIgnoreCase),
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
