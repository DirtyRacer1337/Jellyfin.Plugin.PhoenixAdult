using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace PhoenixAdult
{
    public class PhoenixAdultExternalId : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;
#endif

        public bool Supports(IHasProviderIds item)
            => item is Movie;

#if __EMBY__
#else
        public ExternalIdMediaType? Type
            => ExternalIdMediaType.Movie;
#endif

        public string Key
            => Plugin.Instance.Name;

        public string UrlFormatString
            => null;
    }
/*
    public class PhoenixAdultActorExternalId : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;
#endif

        public bool Supports(IHasProviderIds item)
            => item is Person;

#if __EMBY__

#else
        public ExternalIdMediaType? Type
            => ExternalIdMediaType.Person;
#endif

        public string Key
            => Plugin.Instance.Name + "Actor";

        public string UrlFormatString
            => null;
    }
*/
}
