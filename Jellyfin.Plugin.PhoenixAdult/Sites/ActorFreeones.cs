using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class ActorFreeones : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string actorName, DateTime? actorDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            var url = Helper.GetSearchSearchURL(siteNum) + actorName;
            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            foreach (var actorNode in actorData.SelectNodesSafe("//div[contains(@class, 'grid-item')]"))
            {
                var actorURL = new Uri(Helper.GetSearchBaseURL(siteNum) + actorNode.SelectSingleText(".//a/@href").Replace("/feed", "/bio", StringComparison.OrdinalIgnoreCase));
                string curID = Helper.Encode(actorURL.AbsolutePath),
                    name = actorNode.SelectSingleText(".//p/@title"),
                    imageURL = actorNode.SelectSingleText(".//img/@src");

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = name,
                    ImageUrl = imageURL,
                };

                result.Add(res);
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Person(),
            };

            var actorURL = Helper.Decode(sceneID[0]);
            if (!actorURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                actorURL = Helper.GetSearchBaseURL(siteNum) + actorURL;
            }

            var actorData = await HTML.ElementFromURL(actorURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = actorURL;

            string name = actorData.SelectSingleText("//h1").Replace(" Bio", string.Empty, StringComparison.OrdinalIgnoreCase),
                aliases = actorData.SelectSingleText("//p[contains(., 'Aliases')]/following-sibling::div/p");

            result.Item.OriginalTitle = name + ", " + aliases;
            result.Item.Overview = "\u200B";

            var actorDate = actorData.SelectSingleText("//div[p[contains(., 'Personal Information')]]//span[contains(., 'Born On')]")
                .Replace("Born On", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (DateTime.TryParseExact(actorDate, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var bornPlaceList = new List<string>();
            var bornPlaceNode = actorData.SelectNodesSafe("//div[p[contains(., 'Personal Information')]]//a[@data-test='link-country']/..//span[text()]");
            foreach (var bornPlace in bornPlaceNode)
            {
                var location = bornPlace.InnerText.Trim();

                if (!string.IsNullOrEmpty(location))
                {
                    bornPlaceList.Add(location);
                }
            }

            result.Item.ProductionLocations = new string[] { string.Join(", ", bornPlaceList) };

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var actorURL = Helper.Decode(sceneID[0]);
            if (!actorURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                actorURL = Helper.GetSearchBaseURL(siteNum) + actorURL;
            }

            var actorData = await HTML.ElementFromURL(actorURL, cancellationToken).ConfigureAwait(false);

            var img = actorData.SelectSingleText("//div[contains(@class, 'image-container')]//a/img/@src");
            if (!string.IsNullOrEmpty(img))
            {
                result.Add(new RemoteImageInfo
                {
                    Type = ImageType.Primary,
                    Url = img,
                });
            }

            return result;
        }
    }
}
