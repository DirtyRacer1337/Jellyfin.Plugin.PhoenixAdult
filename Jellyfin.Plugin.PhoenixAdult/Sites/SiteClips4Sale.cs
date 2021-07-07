using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public class SiteClips4Sale : IProviderBase
    {
        private static readonly IList<string> TitleCleanupWords = new List<string>
        {
            "HD", "mp4", "wmv", "360p", "480p", "720p", "1080p", "()", "( - )",
        };

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var parts = searchTitle.Split(' ');
            if (!int.TryParse(parts[0], out var studioId))
            {
                return result;
            }
            else
            {
                searchTitle = string.Join(" ", parts.Skip(1));
            }

            var url = Helper.GetSearchSearchURL(siteNum) + $"{studioId}/*/Cat0-AllCategories/Page1/SortBy-bestmatch/Limit50/search/{searchTitle}";
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[contains(@class, 'clipWrapper')]//section[contains(@id, 'studioListItem')]");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//h3//a/@href"));
                var sceneId = GetSceneIdFromSceneURL(sceneURL.AbsoluteUri);
                string curID = Helper.Encode(sceneURL.PathAndQuery),
                    sceneName = CleanupTitle(searchResult.SelectSingleText(".//h3")),
                    scenePoster = GetPosterUrl(studioId, sceneId);

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                result.Add(res);
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>
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
            result.Item.Name = CleanupTitle(sceneData.SelectSingleText("//h3"));
            result.Item.Overview = sceneData.SelectSingleText("//div[@class='individualClipDescription']");

            result.Item.AddStudio("Clips4Sale");
            var studioName = sceneData.SelectSingleText("//span[contains(text(), 'From:')]/following-sibling::a");
            if (!string.IsNullOrEmpty(studioName))
            {
                result.Item.AddStudio(studioName);
            }

            var sceneDate = sceneData.SelectSingleText("//span[contains(text(), 'Added:')]/span").Split(' ')[0];
            if (DateTime.TryParseExact(sceneDate, "M/d/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var category = sceneData.SelectSingleText("//div[contains(@class, 'clip_details')]//div[contains(., 'Category:')]//a");
            result.Item.AddGenre(category);
            foreach (var genreLink in sceneData.SelectNodesSafe("//span[@class='relatedCatLinks']//a"))
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
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

            var sceneId = GetSceneIdFromSceneURL(sceneURL);
            var studioId = GetStudioIdFromSceneURL(sceneURL);

            var img = GetPosterUrl(studioId, sceneId);
            if (!string.IsNullOrEmpty(img))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Primary,
                });
            }

            return result;
        }

        private static int GetSceneIdFromSceneURL(string sceneUrl)
        {
            return int.Parse(sceneUrl.Split("://").Last().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[3]);
        }

        private static int GetStudioIdFromSceneURL(string sceneUrl)
        {
            return int.Parse(sceneUrl.Split("://").Last().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)[2]);
        }

        private static string GetPosterUrl(int studioId, int sceneId)
        {
            return $"https://imagecdn.clips4sale.com/accounts99/{studioId}/clip_images/previewlg_{sceneId}.jpg";
        }

        private static string CleanupTitle(string title)
        {
            foreach (var word in TitleCleanupWords)
            {
                if (title.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    title = title.Replace(word, string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }

            title = title.Trim();

            if (title.EndsWith("-", StringComparison.OrdinalIgnoreCase))
            {
                title = title.Remove(title.Length - 1, 1);
            }

            return title;
        }
    }
}
