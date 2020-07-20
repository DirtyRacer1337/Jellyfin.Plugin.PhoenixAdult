using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdultNET.Providers;

namespace Jellyfin.Plugin.PhoenixAdult.Providers
{
    public class PhoenixAdultActorImageProvider : IRemoteImageProvider
    {
        public string Name => PhoenixAdultProvider.PluginName;

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();

            if (item == null)
                return images;

            var imageList = await PhoenixAdultNETActorProvider.GetActorPhotos(item.Name, cancellationToken).ConfigureAwait(false);
            if (imageList != null)
                foreach (var image in imageList)
                    images.Add(new RemoteImageInfo
                    {
                        Url = image.Value,
                        Type = ImageType.Primary,
                        ProviderName = image.Key
                    });

            return images;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType> {
                ImageType.Primary
            };

        public bool Supports(BaseItem item) => item is Person;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url
        });
    }
}
