using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;
using SkiaSharp;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class ImageHelper
    {
        public static async Task<RemoteImageInfo> GetImageSizeAndValidate(RemoteImageInfo item, CancellationToken cancellationToken)
        {
            var http = await HTTP.Request(item.Url, HttpMethod.Head, cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                var httpStream = await HTTP.Request(item.Url, cancellationToken).ConfigureAwait(false);
                using (var img = SKBitmap.Decode(httpStream.ContentStream))
                {
                    if (img.Width > 100)
                        return new RemoteImageInfo
                        {
                            ProviderName = item.ProviderName,
                            Url = item.Url,
                            Type = item.Type,
                            Height = img.Height,
                            Width = img.Width
                        };
                }
            }

            return null;
        }
    }
}
