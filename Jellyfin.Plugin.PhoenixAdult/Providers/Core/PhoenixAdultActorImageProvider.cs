using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Providers.Helpers;

#if __EMBY__
using MediaBrowser.Model.Configuration;
#else
#endif

namespace PhoenixAdult
{
    public class PhoenixAdultActorImageProvider : IRemoteImageProvider
    {
        public string Name => Plugin.Instance.Name + "Actor";

        public bool Supports(BaseItem item) => item is Person;

#if __EMBY__
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
#else
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
#endif
        {
            var images = new List<RemoteImageInfo>();

            if (item == null)
                return images;

            var imageList = await GetActorPhotos(item.Name, cancellationToken).ConfigureAwait(false);
            foreach (var image in imageList)
                images.Add(new RemoteImageInfo
                {
                    Url = image.Url,
                    Type = ImageType.Primary,
                    Height = image.Height,
                    Width = image.Width,
                    ProviderName = image.ProviderName
                });

            return images;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType> {
                ImageType.Primary
            };

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url
        });

        public static async Task<List<RemoteImageInfo>> GetActorPhotos(string name, CancellationToken cancellationToken)
        {
            string imageURL;
            var image = new RemoteImageInfo();

            var imageList = new List<RemoteImageInfo>();

            imageURL = await GetFromAdultDVDEmpire(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(imageURL))
            {
                image.Url = imageURL;
                var img = await ImageHelper.GetImageSizeAndValidate(image, cancellationToken).ConfigureAwait(false);
                if (img != null)
                {
                    imageList.Add(new RemoteImageInfo
                    {
                        ProviderName = "AdultDVDEmpire",
                        Url = imageURL,
                        Height = img.Height,
                        Width = img.Width
                    });
                }
            }

            imageURL = await GetFromBoobpedia(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(imageURL))
            {
                image.Url = imageURL;
                var img = await ImageHelper.GetImageSizeAndValidate(image, cancellationToken).ConfigureAwait(false);
                if (img != null)
                {
                    imageList.Add(new RemoteImageInfo
                    {
                        ProviderName = "Boobpedia",
                        Url = imageURL,
                        Height = img.Height,
                        Width = img.Width
                    });
                }
            }

            imageURL = await GetFromBabepedia(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(imageURL))
            {
                image.Url = imageURL;
                var img = await ImageHelper.GetImageSizeAndValidate(image, cancellationToken).ConfigureAwait(false);
                if (img != null)
                {
                    imageList.Add(new RemoteImageInfo
                    {
                        ProviderName = "Babepedia",
                        Url = imageURL,
                        Height = img.Height,
                        Width = img.Width
                    });
                }
            }

            imageURL = await GetFromIAFD(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(imageURL))
            {
                image.Url = imageURL;
                var img = await ImageHelper.GetImageSizeAndValidate(image, cancellationToken).ConfigureAwait(false);
                if (img != null)
                {
                    imageList.Add(new RemoteImageInfo
                    {
                        ProviderName = "IAFD",
                        Url = imageURL,
                        Height = img.Height,
                        Width = img.Width
                    });
                }
            }

            return imageList;
        }

        private static async Task<string> GetFromAdultDVDEmpire(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

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

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//table[@class='infobox']//a[@class='image']//img");
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

            string encodedName = name.Replace(" ", "_", StringComparison.OrdinalIgnoreCase),
                   url = $"https://www.babepedia.com/babe/{encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//div[@id='profimg']/a");
            if (actorImageNode != null)
                image = "https://www.babepedia.com" + actorImageNode.Attributes["href"].Value;

            return image;
        }

        private static async Task<string> GetFromIAFD(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

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
                    image = actorImage;
            }

            return image;
        }
    }
}
