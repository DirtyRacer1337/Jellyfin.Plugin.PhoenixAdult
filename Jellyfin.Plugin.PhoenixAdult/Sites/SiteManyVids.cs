using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class SiteManyVids : IProviderBase
    {
        private const string TitleWatermark = " - Manyvids";
        private readonly Regex tagsListMatch = new Regex("tagsIdsList = \"([\\d,]+)\"", RegexOptions.Compiled);

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var splitedTitle = searchTitle.Split();
            if (!int.TryParse(splitedTitle[0], out var sceneIDx))
            {
                return result;
            }

            var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + $"/Video/{sceneIDx}");
            var sceneID = new List<string> { Helper.Encode(sceneURL.AbsolutePath) };

            if (searchDate.HasValue)
            {
                sceneID.Add(searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID.ToArray(), searchDate, cancellationToken).ConfigureAwait(false);
            if (searchResult.Any())
            {
                result.AddRange(searchResult);
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

            var applicationLD = sceneData.SelectSingleText("//script[@id='applicationLD']");
            var metadata = JsonConvert.DeserializeObject<ManyVidsMetadata>(applicationLD);

            if (metadata.Name.EndsWith(TitleWatermark))
            {
                metadata.Name = metadata.Name.Substring(0, metadata.Name.Length - TitleWatermark.Length);
            }

            result.Item.ExternalId = sceneURL;
            result.Item.Name = metadata.Name;
            result.Item.Overview = metadata.Description;

            result.Item.AddStudio("ManyVids");
            result.Item.AddStudio(metadata.Creator.Name);

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var keywords = await this.GetKeywords(siteNum, sceneData, cancellationToken).ConfigureAwait(false);
            foreach (var genre in keywords)
            {
                result.Item.AddGenre(genre);
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

            var data = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var imgUrl = data.SelectSingleText("//div[@id='rmpPlayer']/@data-video-screenshot");
            if (!string.IsNullOrEmpty(imgUrl))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = imgUrl,
                    Type = ImageType.Primary,
                });
            }

            return result;
        }

        private async Task<IEnumerable<string>> GetKeywords(int[] siteNum, HtmlNode rootNode, CancellationToken cancellationToken)
        {
            var result = Enumerable.Empty<string>();

            var tagsListNode = rootNode.SelectNodesSafe("//script").FirstOrDefault(node => node.InnerHtml.Contains("tagsIdsList"));
            if (tagsListNode == default)
            {
                return result;
            }

            var tagsListMatch = this.tagsListMatch.Match(tagsListNode.InnerHtml);
            if (!tagsListMatch.Success || tagsListMatch.Groups.Count != 2)
            {
                return result;
            }

            var tagsList = tagsListMatch.Groups[1].Captures[0].Value.Split(",").Select(val => int.Parse(val));

            var mvToken = rootNode.SelectSingleText("html/@data-mvtoken");
            var headers = new Dictionary<string, string>
            {
                { "x-requested-with", "XMLHttpRequest" },
            };
            var url = Helper.GetSearchBaseURL(siteNum) + $"/includes/json/vid_categories.php?mvtoken={mvToken}";
            var http = await HTTP.Request(url, cancellationToken, headers).ConfigureAwait(false);
            if (http.IsOK)
            {
                var allKeywords = JsonConvert.DeserializeObject<ManyVidsKeyword[]>(http.Content).ToDictionary(keyword => keyword.Id, keyword => keyword.Label);
                result = tagsList.Select(keywordId => allKeywords[keywordId]);
            }

            return result;
        }

        private class ManyVidsKeyword
        {
            public string Label { get; set; }

            public int Id { get; set; }
        }

        private class ManyVidsMetadata
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public List<string> Keywords { get; set; }

            public ManyVidsMetadataCreator Creator { get; set; }

            internal class ManyVidsMetadataCreator
            {
                public string Name { get; set; }

                public string ProfileUrl { get; set; }
            }
        }
    }
}
