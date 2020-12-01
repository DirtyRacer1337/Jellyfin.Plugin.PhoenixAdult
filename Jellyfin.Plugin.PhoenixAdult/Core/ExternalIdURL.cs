using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace PhoenixAdult
{
    public class ExternalIdURL : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName
            => Plugin.Instance.Name;
#endif

#if __EMBY__
#else
        public ExternalIdMediaType? Type
            => ExternalIdMediaType.Movie;
#endif

        public string Key
            => Plugin.Instance.Name + "URL";

        public string UrlFormatString
            => "{0}";

        public bool Supports(IHasProviderIds item)
            => item is Movie;
    }
}
