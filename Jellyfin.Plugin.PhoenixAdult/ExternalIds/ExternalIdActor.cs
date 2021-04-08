using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace PhoenixAdult
{
    public class ExternalIdActor : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name + " ID Actor";
#else
        public string ProviderName => Plugin.Instance.Name + " ID";
#endif

#if __EMBY__
#else
        public ExternalIdMediaType? Type => ExternalIdMediaType.Person;
#endif

        public string Key => Plugin.Instance.Name + "Actor";

        public string UrlFormatString => null;

        public bool Supports(IHasProviderIds item) => item is Person;
    }
}
