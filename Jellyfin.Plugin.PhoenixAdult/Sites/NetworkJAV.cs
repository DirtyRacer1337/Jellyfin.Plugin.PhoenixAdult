using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

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

            var provider = new Provider(null, Provider.Http);
            var siteList = new List<string> { "JAVLibrary", "R18" };

            foreach (var site in siteList)
            {
                var movieInfo = new MovieInfo
                {
                    Name = $"{site} {searchTitle}",
                    PremiereDate = searchDate,
                };
                var searchResults = await provider.GetSearchResults(movieInfo, cancellationToken).ConfigureAwait(false);

                if ((!searchResults.All(o => o.IndexNumber.HasValue) && searchResults.Any()) || searchResults.Any(o => o.IndexNumber == 100))
                {
                    result.AddRange(searchResults);
                    break;
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            return result;
        }
    }
}
