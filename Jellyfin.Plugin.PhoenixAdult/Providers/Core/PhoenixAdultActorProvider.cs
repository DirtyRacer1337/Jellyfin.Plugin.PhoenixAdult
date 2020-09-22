/*
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;

namespace PhoenixAdult
{
    public class PhoenixAdultActorProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        public string Name => Plugin.Instance.Name + "Actor";

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null)
                return result;

            string encodedName = HttpUtility.UrlEncode(searchInfo.Name),
                   url = $"https://www.adultdvdempire.com/performer/search?q={encodedName}";

            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//div[@id='performerlist']/div//a");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string actorName = searchResult.SelectSingleNode(".//span").InnerText.Trim(),
                        actorImage = searchResult.SelectSingleNode(".//img").Attributes["src"].Value,
                        actorPageURL = $"https://www.adultdvdempire.com{searchResult.Attributes["href"].Value}";

                    result.Add(new RemoteSearchResult
                    {
                        ProviderIds = { { Name, $"0#{PhoenixAdultHelper.Encode(actorPageURL)}" } },
                        Name = actorName,
                        ImageUrl = actorImage
                    });
                }

            return result;
        }

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>()
            {
                HasMetadata = false,
                Item = new Person()
            };

            if (info == null)
                return result;

            result.Item.Name = info.Name;

            var sceneID = info.ProviderIds;
            if (!sceneID.ContainsKey(Name))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                    sceneID = searchResults.First().ProviderIds;
            }

            if (!sceneID.TryGetValue(Name, out string externalID))
                return result;

            var curID = externalID.Split('#');

            var sceneURL = PhoenixAdultHelper.Decode(curID[1]);
            var actorData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = actorData.SelectSingleNode("//h1").InnerText.Trim();
            var descriptionNode = actorData.SelectNodes("(//div[@class='modal-content']/div[contains(@class, 'modal-body') and contains(@class, 'text-md')])[1]/node()[not(self::div) and not(contains(text(), '&copy;'))]");
            if (descriptionNode != null)
            {
                string description = string.Empty;
                foreach (var text in descriptionNode)
                {
                    var t = text.InnerText.Trim();
                    if (!string.IsNullOrEmpty(t))
                        description += t + " ";
                }

                result.Item.Overview = description.Trim();
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url,
            UserAgent = HTTP.GetUserAgent().
        });
    }
}
*/
