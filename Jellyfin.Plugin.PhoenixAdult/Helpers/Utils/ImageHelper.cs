using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;
using SkiaSharp;

internal static class ImageHelper
{
    public static async Task<RemoteImageInfo> GetImageSizeAndValidate(RemoteImageInfo item, CancellationToken cancellationToken)
    {
        var http = await HTTP.Request(new HTTP.HTTPRequest
        {
            _url = item.Url,
            _method = HttpMethod.Head,
        }, cancellationToken).ConfigureAwait(false);
        if (http._response.IsSuccessStatusCode)
        {
            var httpStream = await HTTP.Request(new HTTP.HTTPRequest
            {
                _url = item.Url,
            }, cancellationToken).ConfigureAwait(false);
            using (var img = SKBitmap.Decode(await httpStream._response.Content.ReadAsStreamAsync().ConfigureAwait(false)))
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
