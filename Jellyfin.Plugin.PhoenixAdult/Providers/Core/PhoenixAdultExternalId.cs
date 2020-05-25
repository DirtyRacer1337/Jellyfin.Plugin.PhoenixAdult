using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public class PhoenixAdultExternalId : IExternalId
    {
        public bool Supports(IHasProviderIds item)
            => item is Movie;

        public string Name
            => PhoenixAdultProvider.PluginName;

        public string Key
            => PhoenixAdultProvider.PluginName;

        public string UrlFormatString
            => null;
    }
}
