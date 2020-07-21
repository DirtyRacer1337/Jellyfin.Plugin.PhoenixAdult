using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using PhoenixAdultNET.Providers;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public class PhoenixAdultProvider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => "PhoenixAdult";

        public static string PluginName { get; set; }
        public static ILogger<PhoenixAdultProvider> Log { get; set; }
        public static IHttpClient Http { get; set; }
        public static readonly CultureInfo Lang = new CultureInfo("en-US", false);

        public PhoenixAdultProvider(ILogger<PhoenixAdultProvider> logger, IHttpClient http)
        {
            PluginName = Name;
            Log = logger;
            Http = http;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            List<RemoteSearchResult> result = new List<RemoteSearchResult>();

            if (searchInfo == null)
                return result;

            var searchResults = await PhoenixAdultNETProvider.Search(searchInfo.Name, cancellationToken).ConfigureAwait(false);
            foreach (var searchResult in searchResults)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { PluginName, searchResult.CurID } },
                    Name = searchResult.Title,
                    PremiereDate = searchResult.ReleaseDate,
                    ImageUrl = searchResult.Poster
                });
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>
            {
                HasMetadata = false,
                Item = new Movie(),
                People = new List<PersonInfo>()
            };

            if (info == null)
                return result;

            var sceneID = info.ProviderIds;
            if (!sceneID.ContainsKey(Name))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                    sceneID = searchResults.First().ProviderIds;
            }

            var externalID = sceneID.GetValueOrDefault(Name);
            if (string.IsNullOrEmpty(externalID))
                return result;

            var curID = externalID.Split('#');
            if (curID.Length < 3)
                return result;

            Log.LogInformation($"PhoenixAdult ID: {externalID}");
            var scene = await PhoenixAdultNETProvider.Update(externalID, cancellationToken).ConfigureAwait(false);
            if (scene != null)
            {
                result.HasMetadata = true;
                result.Item.OfficialRating = "XXX";
                result.Item.ProviderIds = sceneID;

                result.Item.Name = scene.Title;
                result.Item.Overview = scene.Description;

                if (scene.Studios != null)
                    foreach (var studio in scene.Studios)
                        result.Item.AddStudio(studio);

                if (scene.ReleaseDate.HasValue)
                {
                    result.Item.PremiereDate = scene.ReleaseDate;
                    result.Item.ProductionYear = scene.ReleaseDate.Value.Year;
                }

                if (scene.Genres != null)
                    foreach (var genreName in scene.Genres)
                        result.Item.AddGenre(genreName);

                if (scene.Actors != null)
                    foreach (var actorLink in scene.Actors)
                    {
                        result.People.Add(new PersonInfo
                        {
                            Name = actorLink.Name,
                            ImageUrl = actorLink.Photo,
                            Type = PersonType.Actor
                        });
                    }
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => Http.GetResponse(new HttpRequestOptions
        {
            CancellationToken = cancellationToken,
            Url = url
        });
    }
}
