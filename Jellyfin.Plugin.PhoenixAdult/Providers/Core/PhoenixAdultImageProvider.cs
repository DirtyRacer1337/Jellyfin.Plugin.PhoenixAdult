using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Core
{
    public class PhoenixAdultImageProvider : IRemoteImageProvider
    {
        public string Name => PhoenixAdultProvider.PluginName;

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();

            if (item == null)
                return images;

            var externalID = item.ProviderIds.GetValueOrDefault(Name);
            if (string.IsNullOrEmpty(externalID))
                return images;

            var curID = externalID.Split('#');
            if (curID.Length < 3)
                return images;

            var provider = PhoenixAdultList.GetProviderBySiteID(int.Parse(curID[0], PhoenixAdultHelper.Lang));
            if (provider != null)
            {
                images = (List<RemoteImageInfo>)await provider.GetImages(item, cancellationToken).ConfigureAwait(false);

                var clearList = new List<RemoteImageInfo>();
                foreach (var image in images)
                {
                    var http = await image.Url.AllowAnyHttpStatus().HeadAsync(cancellationToken).ConfigureAwait(false);
                    if (http.IsSuccessStatusCode)
                    {
                        var img = Image.FromStream(await image.Url.GetStreamAsync(cancellationToken).ConfigureAwait(false));

                        if (img.Width > 100)
                            clearList.Add(image);
                    }
                }

                images = clearList;
            }

            return images;
        }

        public bool Supports(BaseItem item) => item is Movie;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultHelper.GetImageResponse(url, cancellationToken);

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType> {
                    ImageType.Primary,
                    ImageType.Backdrop
            };
    }
}
