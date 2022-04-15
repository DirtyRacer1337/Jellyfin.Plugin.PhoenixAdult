using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Providers;

#if __EMBY__
using MediaBrowser.Model.Configuration;
#endif

namespace PhoenixAdult.Sites
{
    public class NetworkJAV : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
            {
                return result;
            }

            var provider = new MovieProvider();
            var siteList = new List<string> { "JAVLibrary", "R18" };

            foreach (var site in siteList)
            {
                var searchInfo = new MovieInfo
                {
                    Name = $"{site} {searchTitle}",
                    PremiereDate = searchDate,
                };
                var searchResults = await provider.GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);

                if ((!searchResults.All(o => o.IndexNumber.HasValue) && searchResults.Any()) || searchResults.Any(o => o.IndexNumber == 100))
                {
                    result.AddRange(searchResults);
                    break;
                }
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            var provider = new MovieProvider();
            var info = new MovieInfo();
            info.ProviderIds[Plugin.Instance.Name] = string.Join("#", sceneID);

            var res = await provider.GetMetadata(info, cancellationToken).ConfigureAwait(false);
            result.Item = res.Item;

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var provider = new MovieImageProvider();
            var info = new Movie();
            info.ProviderIds[Plugin.Instance.Name] = string.Join("#", sceneID);

#if __EMBY__
            result = (await provider.GetImages(info, new LibraryOptions(), cancellationToken).ConfigureAwait(false)).ToList();
#else
            result = (await provider.GetImages(info, cancellationToken).ConfigureAwait(false)).ToList();
#endif

            return result;
        }
    }
}
