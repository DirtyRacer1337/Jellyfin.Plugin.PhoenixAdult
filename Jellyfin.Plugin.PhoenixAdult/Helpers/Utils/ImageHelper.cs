using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using SkiaSharp;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class ImageHelper
    {
        public static async Task<List<RemoteImageInfo>> GetImagesSizeAndValidate(IEnumerable<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();
            var tasks = new List<Task<RemoteImageInfo>>();

            var cleanImages = Cleanup(images);

            var primaryList = cleanImages.Where(o => o.Type == ImageType.Primary).ToList();
            var backdropList = cleanImages.Where(o => o.Type == ImageType.Backdrop).ToList();
            var dublList = new List<RemoteImageInfo>();

            foreach (var image in primaryList)
            {
                tasks.Add(GetImageSizeAndValidate(image, cancellationToken));
            }

            foreach (var image in backdropList)
            {
                if (!primaryList.Where(o => o.Url == image.Url).Any())
                {
                    tasks.Add(GetImageSizeAndValidate(image, cancellationToken));
                }
                else
                {
                    dublList.Add(image);
                }
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetImagesSizeAndValidate error: \"{e}\"");

                await Analytics.Send(
                    new AnalyticsExeption
                    {
                        Request = string.Join(" | ", cleanImages.Select(o => o.Url)),
                    }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (var task in tasks)
                {
                    var res = task.Result;

                    if (res != null)
                    {
                        result.Add(res);
                    }
                }
            }

            if (result.Any())
            {
                foreach (var image in dublList)
                {
                    var res = result.Where(o => o.Url == image.Url);
                    if (res.Any())
                    {
                        var img = res.First();

                        result.Add(new RemoteImageInfo
                        {
                            ProviderName = image.ProviderName,
                            Url = image.Url,
                            Type = ImageType.Backdrop,
                            Height = img.Height,
                            Width = img.Width,
                        });
                    }
                }
            }

            return result;
        }

        private static List<RemoteImageInfo> Cleanup(IEnumerable<RemoteImageInfo> images)
        {
            var clearImages = new List<RemoteImageInfo>();

            foreach (var image in images)
            {
                if (!clearImages.Where(o => o.Url == image.Url && o.Type == image.Type).Any())
                {
                    if (string.IsNullOrEmpty(image.ProviderName))
                    {
                        image.ProviderName = Plugin.Instance.Name;
                    }

                    clearImages.Add(image);
                }
            }

            var backdrops = clearImages.Where(o => o.Type == ImageType.Backdrop);
            if (backdrops.Any())
            {
                var firstBackdrop = backdrops.First();
                if (firstBackdrop != null && clearImages.Where(o => o.Type == ImageType.Primary).First().Url == firstBackdrop.Url)
                {
                    clearImages.Remove(firstBackdrop);
                    clearImages.Add(firstBackdrop);
                }
            }

            return clearImages;
        }

        private static async Task<RemoteImageInfo> GetImageSizeAndValidate(RemoteImageInfo item, CancellationToken cancellationToken)
        {
            if (Plugin.Instance.Configuration.DisableImageValidation)
            {
                return item;
            }

            var http = await HTTP.Request(item.Url, HttpMethod.Head, cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                if (Plugin.Instance.Configuration.DisableImageSize)
                {
                    return item;
                }

                http = await HTTP.Request(item.Url, cancellationToken).ConfigureAwait(false);
                if (http.IsOK)
                {
                    SKImage img = null;

                    try
                    {
                        img = SKImage.FromEncodedData(http.ContentStream);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"ImageHelper error: \"{e}\"");
                    }

                    if (img != null && img.Width > 100)
                    {
                        return new RemoteImageInfo
                        {
                            ProviderName = item.ProviderName,
                            Url = item.Url,
                            Type = item.Type,
                            Height = img.Height,
                            Width = img.Width,
                        };
                    }
                }
            }

            return null;
        }
    }
}
