using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers.Utils;

#if __EMBY__
using MediaBrowser.Model.Configuration;
#else
#endif

namespace PhoenixAdult
{
    public class ActorImageProvider : IRemoteImageProvider
    {
        public string Name => Plugin.Instance.Name + "Actor";

        public static async Task<List<RemoteImageInfo>> GetActorPhotos(string name, CancellationToken cancellationToken)
        {
            var tasks = new Dictionary<string, Task<string>>();
            var imageList = new List<RemoteImageInfo>();

            Logger.Info($"Searching actor images for \"{name}\"");

            tasks.Add("AdultDVDEmpire", GetFromAdultDVDEmpire(name, cancellationToken));
            tasks.Add("Boobpedia", GetFromBoobpedia(name, cancellationToken));
            tasks.Add("Babepedia", GetFromBabepedia(name, cancellationToken));
            tasks.Add("IAFD", GetFromIAFD(name, cancellationToken));

            try
            {
                await Task.WhenAll(tasks.Values).ConfigureAwait(false);
            }
            catch (AggregateException e)
            {
                Logger.Error(e.Message);
            }
            finally
            {
                foreach (var image in tasks)
                {
                    var res = image.Value.Result;

                    if (!string.IsNullOrEmpty(res))
                    {
                        imageList.Add(new RemoteImageInfo
                        {
                            ProviderName = image.Key,
                            Url = res,
                        });
                    }
                }
            }

            return imageList;
        }

        public bool Supports(BaseItem item) => item is Person;

#if __EMBY__
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
#else
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
#endif
        {
            var images = new List<RemoteImageInfo>();

            if (item == null)
            {
                return images;
            }

            images = await GetActorPhotos(item.Name, cancellationToken).ConfigureAwait(false);
            images = await ImageHelper.GetImagesSizeAndValidate(images, cancellationToken).ConfigureAwait(false);

            if (images.Any())
            {
                images = images.OrderByDescending(o => o.Height).ToList();
            }

            return images;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType>
        {
            ImageType.Primary,
        };

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => Provider.Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url,
            EnableDefaultUserAgent = false,
            UserAgent = HTTP.GetUserAgent(),
        });

        private static async Task<string> GetFromAdultDVDEmpire(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
            {
                return image;
            }

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"https://www.adultdvdempire.com/performer/search?q={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorNode = actorData.SelectSingleNode("//div[@id='performerlist']/div//a");
            if (actorNode != null)
            {
                var actorPageURL = "https://www.adultdvdempire.com" + actorNode.Attributes["href"].Value;
                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var img = actorPage.SelectSingleNode("//div[contains(@class, 'performer-image-container')]/a");
                if (img != null)
                {
                    image = img.Attributes["href"].Value;
                }
            }

            return image;
        }

        private static async Task<string> GetFromBoobpedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
            {
                return image;
            }

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.boobpedia.com/wiki/index.php?search={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//table[@class='infobox']//a[@class='image']//img");
            if (actorImageNode != null)
            {
                var img = actorImageNode.Attributes["src"].Value;
                if (!img.Contains("NoImage", StringComparison.OrdinalIgnoreCase))
                {
                    image = "http://www.boobpedia.com" + actorImageNode.Attributes["src"].Value;
                }
            }

            return image;
        }

        private static async Task<string> GetFromBabepedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
            {
                return image;
            }

            string encodedName = name.Replace(" ", "_", StringComparison.OrdinalIgnoreCase),
                   url = $"https://www.babepedia.com/babe/{encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//div[@id='profimg']/a");
            if (actorImageNode != null)
            {
                var img = actorImageNode.Attributes["href"].Value;
                if (!img.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    image = "https://www.babepedia.com" + img;
                }
            }

            return image;
        }

        private static async Task<string> GetFromIAFD(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
            {
                return image;
            }

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.iafd.com/results.asp?searchtype=comprehensive&searchstring={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorNode = actorData.SelectSingleNode("//table[@id='tblFem']//tbody//a");
            if (actorNode != null)
            {
                var actorPageURL = "http://www.iafd.com" + actorNode.Attributes["href"].Value;
                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorImage = actorPage.SelectSingleNode("//div[@id='headshot']//img").Attributes["src"].Value;
                if (!actorImage.Contains("nophoto", StringComparison.OrdinalIgnoreCase))
                {
                    image = actorImage;
                }
            }

            return image;
        }
    }
}
