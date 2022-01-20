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
    public class SiteData18 : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//span[@class='gen12' and contains(., 'Movies:')]/following-sibling::div[text()] | //b[contains(., 'Content:')]/following-sibling::div//div[@class='contenedor']/div");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(searchResult.SelectSingleText(".//a/@href"));
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText(".//img/@title"),
                    scenePoster = searchResult.SelectSingleText(".//img/@src"),
                    sceneDate = searchResult.SelectSingleText(".//p[@class='genmed'] | ./text()");

                var res = new RemoteSearchResult
                {
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (!string.IsNullOrEmpty(sceneDate))
                {
                    sceneDate = sceneDate.Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase)
                    .Replace("June", "Jun", StringComparison.OrdinalIgnoreCase)
                    .Replace("July", "Jul", StringComparison.OrdinalIgnoreCase)
                    .Replace("March", "Mar", StringComparison.OrdinalIgnoreCase)
                    .Trim();
                }

                if (DateTime.TryParseExact(sceneDate, "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    res.PremiereDate = sceneDateObj;
                }
                else
                {
                    if (DateTime.TryParseExact(sceneDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                    }
                    else
                    {
                        if (searchDate.HasValue)
                        {
                            res.PremiereDate = searchDate.Value;
                        }
                    }
                }

                if (res.PremiereDate.HasValue)
                {
                    curID += $"#{res.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
                }

                res.ProviderIds.Add(Plugin.Instance.Name, curID);

                result.Add(res);
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

            string sceneURL = Helper.Decode(sceneID[0]),
                sceneDate = string.Empty;

            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            if (sceneID.Length > 1)
            {
                sceneDate = sceneID[1];
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//h1");
            result.Item.Overview = sceneData.SelectSingleText("//p[contains(., 'Description:') or contains(., 'Story:')]")
                .Replace("Description:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Story:", string.Empty, StringComparison.OrdinalIgnoreCase);

            result.Item.AddStudio("Data18");
            var studio = sceneData.SelectSingleText("//p[contains(., 'Studio:') or contains(., 'Site:')]/a/text()");
            if (!string.IsNullOrEmpty(studio))
            {
                result.Item.AddStudio(studio);
            }

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }
            else
            {
                var date = sceneData.SelectSingleText("//span[contains(., 'Release date:') and not(contains(., 'No date release yet'))]/a");
                if (!string.IsNullOrEmpty(date) && DateTime.TryParseExact(date, "MMMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodesSafe("//*[contains(., 'Categories:')]/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[@class='contenedor']//img | //ul/li//img");
            foreach (var actorLink in actorsNode)
            {
                var actor = new PersonInfo
                {
                    Name = actorLink.SelectSingleText("./@alt"),
                    ImageUrl = actorLink.SelectSingleText("./@src"),
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

            foreach (var node in sceneData.SelectNodesSafe("//a[@data-featherlight='image']"))
            {
                var img = node.SelectSingleText("./@href");

                result.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Primary,
                });
                result.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Backdrop,
                });
            }

            return result;
        }
    }
}
