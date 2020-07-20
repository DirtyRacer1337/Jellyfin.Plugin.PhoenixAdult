using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdultNET.Providers;

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

            var scene = await PhoenixAdultNETProvider.Update(externalID, cancellationToken).ConfigureAwait(false);
            if (scene != null)
            {
                if (scene.Posters != null)
                    foreach (var poster in scene.Posters)
                        images.Add(new RemoteImageInfo
                        {
                            Url = poster,
                            Type = ImageType.Primary,
                            ProviderName = PhoenixAdultProvider.PluginName
                        });

                if (scene.Backgrounds != null)
                    foreach (var background in scene.Backgrounds)
                        images.Add(new RemoteImageInfo
                        {
                            Url = background,
                            Type = ImageType.Backdrop,
                            ProviderName = PhoenixAdultProvider.PluginName
                        });
            }

            return images;
        }

        public bool Supports(BaseItem item) => item is Movie;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url
        });

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType> {
                    ImageType.Primary,
                    ImageType.Backdrop
            };
    }
}
