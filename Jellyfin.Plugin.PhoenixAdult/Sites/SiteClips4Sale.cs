using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class SiteClips4Sale : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var firstSpaceIndex = searchTitle.IndexOf(' ');
            if (firstSpaceIndex == -1 || !int.TryParse(searchTitle.Substring(0, firstSpaceIndex), out var studioId))
            {
                // Studio id not specified
                return result;
            }

            var encodedSearchTitle = Uri.EscapeUriString(searchTitle.Substring(firstSpaceIndex + 1));
            var url = Helper.GetSearchSearchURL(siteNum) + studioId + "/*/Cat0-AllCategories/Page1/SortBy-bestmatch/Limit50/search/" + encodedSearchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            // Clips4Sale indicates the number of results, but will return 50 items of the studio even if they do not match
            var resultsCount = int.Parse(data.SelectSingleNode("//div[@id=\"view-searchInfo\"]/strong").InnerText.Trim());

            var searchResults = data.SelectNodesSafe("//div[contains(@class, \"clipWrapper\")]//section[@class=\"p-0\"]");
            for (var i = 0; i < resultsCount && i < searchResults.Count; ++i)
            {
                var searchResult = searchResults[i];
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//h3//a/@href"));
                var sceneId = GetSceneIdFromSceneUrl(sceneURL);
                var titleNoFormatting = CleanupTitle(searchResult.SelectSingleText(".//h3"));
                var curID = Helper.Encode(sceneURL.PathAndQuery);
                var scenePoster = GetPosterUrl(studioId, sceneId);

                var res = new RemoteSearchResult
                {
                    ProviderIds =
                    {
                        {
                            Plugin.Instance.Name, curID
                        },
                    }, Name = titleNoFormatting, ImageUrl = scenePoster,
                };

                result.Add(res);
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>
            {
                Item = new Movie(), People = new List<PersonInfo>(),
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
            result.Item.Name = CleanupTitle(sceneData.SelectSingleText("//h3"));

            var summary = HttpUtility.HtmlDecode(sceneData.SelectSingleText("//div[@class=\"individualClipDescription\"]")).Trim();
            result.Item.Overview = summary;

            result.Item.AddStudio("Clips4Sale");
            var studioName = sceneData.SelectSingleText("//span[contains(text(),\"From:\")]/following-sibling::a");
            result.Item.AddStudio(studioName);

            var sceneDate = sceneData.SelectSingleText("//span[contains(text(),\"Added:\")]/span").Split(' ')[0];
            if (DateTime.TryParseExact(sceneDate, "M/d/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var category = sceneData.SelectSingleText("//div[contains(@class, \"clip_details\")]//div[contains(., \"Category:\")]//a").Trim();
            result.Item.AddGenre(category.ToLower());
            foreach (var relatedCategoryNode in sceneData.SelectNodes("//span[@class=\"relatedCatLinks\"]//a"))
            {
                result.Item.AddGenre(relatedCategoryNode.InnerText.Trim().ToLower());
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

            var sceneURL = new Uri(Helper.Decode(sceneID[0]));
            var sceneId = GetSceneIdFromSceneUrl(sceneURL);
            var studioId = int.Parse(sceneURL.AbsolutePath.Split(new [] { '/' }, StringSplitOptions.RemoveEmptyEntries)[1]);

            var img = GetPosterUrl(studioId, sceneId);
            if (!string.IsNullOrEmpty(img))
            {
                result.Add(new RemoteImageInfo { Url = img, Type = ImageType.Primary });
            }

            return result;
        }

        private static int GetSceneIdFromSceneUrl(Uri sceneUrl)
        {
            return int.Parse(sceneUrl.AbsolutePath.Split(new [] { '/' }, StringSplitOptions.RemoveEmptyEntries)[2]);
        }

        private static string GetPosterUrl(int studioId, int sceneId)
        {
            return $"https://imagecdn.clips4sale.com/accounts99/{studioId}/clip_images/previewlg_{sceneId}.jpg";
        }

        private static string CleanupTitle(string title)
        {
            return title.Replace("(HD MP4)", string.Empty).Replace("(WMV)", string.Empty).Trim();
        }
    }
}
