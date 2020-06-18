using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using HtmlAgilityPack;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Core
{
    public class PhoenixAdultActorImageProvider : IRemoteImageProvider
    {
        public string Name => PhoenixAdultProvider.PluginName;

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();

            if (item == null)
                return images;

            string image;

            image = await GetFromAdultDVDEmpire(item.Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                images.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Primary,
                    ProviderName = "AdultDVDEmpire"
                });

            image = await GetFromBoobpedia(item.Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                images.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Primary,
                    ProviderName = "Boobpedia"
                });

            image = await GetFromBabepedia(item.Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                images.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Primary,
                    ProviderName = "Babepedia"
                });

            image = await GetFromIAFD(item.Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                images.Add(new RemoteImageInfo
                {
                    Url = image,
                    Type = ImageType.Primary,
                    ProviderName = "IAFD"
                });

            return images;
        }

        public bool Supports(BaseItem item) => item is Person;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultHelper.GetImageResponse(url, cancellationToken);

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType> {
                    ImageType.Primary
            };

        private static async Task<string> GetFromAdultDVDEmpire(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"https://www.adultdvdempire.com/performer/search?q={encodedName}";

            var http = await url.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

            var actorNode = html.DocumentNode.SelectSingleNode("//div[@id='performerlist']/div//a");
            if (actorNode != null)
            {
                var actorPageURL = "https://www.adultdvdempire.com" + actorNode.Attributes["href"].Value;

                http = await actorPageURL.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").GetAsync(cancellationToken).ConfigureAwait(false);
                html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

                var img = html.DocumentNode.SelectSingleNode("//div[contains(@class, 'performer-image-container')]/a");
                if (img != null)
                    image = img.Attributes["href"].Value;
            }

            return image;
        }

        private static async Task<string> GetFromBoobpedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.boobpedia.com/wiki/index.php?search={encodedName}";

            var http = await url.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

            var actorImageNode = html.DocumentNode.SelectSingleNode("//table[@class='infobox']//a[@class='image']//img");
            if (actorImageNode != null)
            {
                var img = actorImageNode.Attributes["src"].Value;
                if (!img.Contains("NoImage", StringComparison.OrdinalIgnoreCase))
                    image = "http://www.boobpedia.com" + actorImageNode.Attributes["src"].Value;
            }

            return image;
        }

        private static async Task<string> GetFromBabepedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string actorImage = $"http://www.babepedia.com/pics/{name}.jpg";

            var http = await actorImage.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").HeadAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
                image = actorImage;

            return image;
        }

        private static async Task<string> GetFromIAFD(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.iafd.com/results.asp?searchtype=comprehensive&searchstring={encodedName}";

            var http = await url.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

            var actorNode = html.DocumentNode.SelectSingleNode("//table[@id='tblFem']//tbody//a");
            if (actorNode != null)
            {
                var actorPageURL = "http://www.iafd.com" + actorNode.Attributes["href"].Value;
                http = await actorPageURL.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").GetAsync(cancellationToken).ConfigureAwait(false);
                html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

                var actorImage = html.DocumentNode.SelectSingleNode("//div[@id='headshot']//img").Attributes["src"].Value;
                if (!actorImage.Contains("nophoto", StringComparison.OrdinalIgnoreCase))
                    image = actorImage;
            }

            return image;
        }
    }
}
