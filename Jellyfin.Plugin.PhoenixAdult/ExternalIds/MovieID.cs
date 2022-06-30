using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace PhoenixAdult.ExternalId
{
    public class MovieID : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name + " ID";
#else
        public string ProviderName => Plugin.Instance.Name + " ID";

        public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;
#endif

        public string Key => Plugin.Instance.Name;

        public string UrlFormatString => null;

        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
