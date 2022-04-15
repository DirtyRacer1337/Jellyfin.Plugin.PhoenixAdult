using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Configuration;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
#else
using System.Net.Http;
#endif

namespace PhoenixAdult.Providers
{
    public class ActorImageProvider : IRemoteImageProvider
    {
        public string Name => Plugin.Instance.Name;

        public static async Task<List<RemoteImageInfo>> GetActorPhotos(string name, CancellationToken cancellationToken)
        {
            var tasks = new Dictionary<string, Task<string>>();
            var imageList = new List<RemoteImageInfo>();

            if (string.IsNullOrEmpty(name))
            {
                return imageList;
            }

            Logger.Info($"Searching actor images for \"{name}\"");

            tasks.Add("AdultDVDEmpire", GetFromAdultDVDEmpire(name, cancellationToken));
            tasks.Add("Boobpedia", GetFromBoobpedia(name, cancellationToken));
            tasks.Add("Babepedia", GetFromBabepedia(name, cancellationToken));
            tasks.Add("IAFD", GetFromIAFD(name, cancellationToken));

            var splitedName = name.Split();
            switch (Plugin.Instance.Configuration.JAVActorNamingStyle)
            {
                case JAVActorNamingStyle.JapaneseStyle:
                    if (splitedName.Length > 1)
                    {
                        var nameReversed = string.Join(" ", splitedName.Reverse());

                        tasks.Add("Boobpedia 2", GetFromBoobpedia(nameReversed, cancellationToken));
                        tasks.Add("Babepedia 2", GetFromBabepedia(nameReversed, cancellationToken));
                        tasks.Add("IAFD 2", GetFromIAFD(nameReversed, cancellationToken));
                    }

                    break;
            }

            try
            {
                await Task.WhenAll(tasks.Values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetActorPhotos error: \"{e}\"");

                await Analytics.Send(
                    new AnalyticsExeption
                    {
                        Request = name,
                        Exception = e,
                    }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (var image in tasks)
                {
                    var res = image.Value.Result;

                    if (!string.IsNullOrEmpty(res) && !imageList.Where(o => o.Url == res).Any())
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

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
            => new List<ImageType>
            {
                ImageType.Primary,
            };

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

            if (item.ProviderIds.TryGetValue(this.Name, out var externalID))
            {
                var curID = externalID.Split('#');
                if (curID.Length > 2)
                {
                    var siteNum = new int[2] { int.Parse(curID[0], CultureInfo.InvariantCulture), int.Parse(curID[1], CultureInfo.InvariantCulture) };
                    var sceneID = item.ProviderIds;

                    if (sceneID.ContainsKey(this.Name))
                    {
                        var provider = Helper.GetProviderBySiteID(siteNum[0]);
                        if (provider != null)
                        {
                            IEnumerable<RemoteImageInfo> imgs = new List<RemoteImageInfo>();
                            try
                            {
                                imgs = await provider.GetImages(siteNum, curID.Skip(2).ToArray(), item, cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"GetImages error: \"{e}\"");

                                await Analytics.Send(
                                    new AnalyticsExeption
                                    {
                                        Request = string.Join("#", curID.Skip(2)),
                                        SiteNum = siteNum,
                                        Exception = e,
                                    }, cancellationToken).ConfigureAwait(false);
                            }
                            finally
                            {
                                if (imgs.Any())
                                {
                                    images.AddRange(imgs);
                                }
                            }
                        }
                    }
                }
            }

            images = await ImageHelper.GetImagesSizeAndValidate(images, cancellationToken).ConfigureAwait(false);

            if (images.Any())
            {
                foreach (var img in images)
                {
                    if (string.IsNullOrEmpty(img.ProviderName))
                    {
                        img.ProviderName = this.Name;
                    }
                }

                images = images.OrderByDescending(o => o.Height).ToList();
            }

            return images;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
#endif
        {
            return Helper.GetImageResponse(url, cancellationToken);
        }

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

            var actorPageURL = actorData.SelectSingleText("//div[@id='performerlist']/div//a/@href");
            if (!string.IsNullOrEmpty(actorPageURL))
            {
                if (!actorPageURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    actorPageURL = "https://www.adultdvdempire.com" + actorPageURL;
                }

                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var img = actorPage.SelectSingleText("//div[contains(@class, 'performer-image-container')]/a/@href");
                if (!string.IsNullOrEmpty(img))
                {
                    image = img;
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

            var img = actorData.SelectSingleText("//table[@class='infobox']//a[@class='image']//img/@src");
            if (!string.IsNullOrEmpty(img) && !img.Contains("NoImage", StringComparison.OrdinalIgnoreCase))
            {
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = "http://www.boobpedia.com" + img;
                }

                image = img;
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

            var img = actorData.SelectSingleText("//div[@id='profimg']/a/@href");
            if (!string.IsNullOrEmpty(img) && !img.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = "https://www.babepedia.com" + img;
                }

                image = img;
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

            var actorPageURL = actorData.SelectSingleText("//table[@id='tblFem']//tbody//a/@href");
            if (!string.IsNullOrEmpty(actorPageURL))
            {
                if (!actorPageURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    actorPageURL = "http://www.iafd.com" + actorPageURL;
                }

                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorImage = actorPage.SelectSingleText("//div[@id='headshot']//img/@src");
                if (!actorImage.Contains("nophoto", StringComparison.OrdinalIgnoreCase))
                {
                    image = actorImage;
                }
            }

            return image;
        }
    }
}
