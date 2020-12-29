using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace PhoenixAdult
{
    internal interface IProviderBaseActor
    {
        Task<List<RemoteSearchResult>> Search(int[] siteNum, string actorName, CancellationToken cancellationToken);

        Task<MetadataResult<Person>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken);

        Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken);
    }
}
