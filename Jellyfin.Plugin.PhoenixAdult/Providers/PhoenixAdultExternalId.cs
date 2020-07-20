using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public class PhoenixAdultExternalId : IExternalId
    {
        public string ProviderName => PhoenixAdultProvider.PluginName;

        public bool Supports(IHasProviderIds item)
            => item is Movie;

        public ExternalIdMediaType? Type => null;

        public string Key
            => PhoenixAdultProvider.PluginName;

        public string UrlFormatString
            => null;
    }
}
