using System;
using System.Collections.Generic;
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
    public class SiteManyvids : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>(1);
            if (siteNum == null || string.IsNullOrEmpty(searchTitle) || !int.TryParse(searchTitle, out _))
            {
                return result;
            }

            var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + $"/Video/{searchTitle}");
            var sceneID = new[] { Helper.Encode(sceneURL.AbsolutePath) };

            return await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, searchDate, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var data = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var applicationLD = data.SelectSingleNode("//script[@id=\"applicationLD\"]");
            var metadata = JsonConvert.DeserializeObject<ManyvidsMetadata>(applicationLD.InnerText);

            const string titleWatermark = " - Manyvids";
            if (metadata.Name.EndsWith(titleWatermark))
            {
                metadata.Name = metadata.Name.Substring(0, metadata.Name.Length - titleWatermark.Length);
            }

            result.Item.ExternalId = sceneID[0];
            result.Item.Name = metadata.Name;
            result.Item.Overview = metadata.Description;

            result.Item.AddStudio("Manyvids");
            result.Item.AddStudio(metadata.Creator.Name);

            var keywords = await GetKeywords(data, cancellationToken);
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

            var rpmPlayerNode = data.SelectSingleNode("//div[@id=\"rmpPlayer\"]");
            var imgUrl = rpmPlayerNode.Attributes["data-video-screenshot"].Value;

            result.Add(new RemoteImageInfo
            {
                Url = imgUrl,
                Type = ImageType.Primary,
            });

            return result;
        }

        private static async Task<IEnumerable<string>> GetKeywords(HtmlNode rootNode, CancellationToken cancellationToken)
        {
            var tagsListNode = rootNode.SelectNodes("//script").FirstOrDefault(node => node.InnerHtml.Contains("tagsIdsList"));
            if (tagsListNode == default)
            {
                return Enumerable.Empty<string>();
            }

            var tagsListMatch = new Regex("tagsIdsList = \"([\\d,]+)\"").Match(tagsListNode.InnerHtml);
            if (!tagsListMatch.Success || tagsListMatch.Groups.Count != 2)
            {
                return Enumerable.Empty<string>();
            }

            var tagsList = tagsListMatch.Groups[1].Captures[0].Value.Split(",").Select(val => int.Parse(val));

            var mvToken = rootNode.SelectSingleNode("html").Attributes["data-mvtoken"].Value;
            var headers = new Dictionary<string, string>
            {
                ["x-requested-with"] = "XMLHttpRequest",
            };
            var allKeywordsResponse = await HTTP.Request($"https://www.manyvids.com/includes/json/vid_categories.php?mvtoken={mvToken}", cancellationToken, headers);
            var allKeywords = JsonConvert.DeserializeObject<ManyvidsKeyword[]>(allKeywordsResponse.Content).ToDictionary(keyword => keyword.Id, keyword => keyword.Label);

            return tagsList.Select(keywordId => allKeywords[keywordId]);
        }

        private class ManyvidsKeyword
        {
            public string Label { get; set; }

            public int Id { get; set; }
        }

        private class ManyvidsMetadata
        {
            public string Name { get; set; }

            public string Description { get; set; }

            public List<string> Keywords { get; set; }

            public ManyvidsMetadataCreator Creator { get; set; }

            internal class ManyvidsMetadataCreator
            {
                public string Name { get; set; }

                public string ProfileUrl { get; set; }
            }
        }
    }
}
