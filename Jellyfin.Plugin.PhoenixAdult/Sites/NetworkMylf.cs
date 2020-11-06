using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class NetworkMylf : IProviderBase
    {
        private static readonly Dictionary<string, string[]> Genres = new Dictionary<string, string[]>
        {
            { "MylfBoss", new[] { "Office", "Boss" } },
            { "MylfBlows", new[] { "Blowjob" } },
            { "Milfty", new[] { "Cheating" } },
            { "Mom Drips", new[] { "Creampie" } },
            { "Milf Body", new[] { "Gym", "Fitness" } },
            { "Lone Milf", new[] { "Solo" } },
            { "Full Of JOI", new[] { "JOI" } },
            { "Mylfed", new[] { "Lesbian" } },
            { "MylfDom", new[] { "BDSM" } },
            { "Sis Loves Me", new[] { "Step Sister" } },
            { "DadCrush", new[] { "Step Dad", "Step Daughter" } },
            { "DaughterSwap", new[] { "Step Dad", "Step Daughter" } },
            { "PervMom", new[] { "Step Mom" } },
            { "Family Strokes", new[] { "Taboo Family" } },
            { "Foster Tapes", new[] { "Taboo Sex" } },
            { "BFFs", new[] { "Teen", "Group Sex" } },
            { "Shoplyfter", new[] { "Strip" } },
            { "ShoplyfterMylf", new[] { "Strip", "MILF" } },
            { "Exxxtra Small", new[] { "Teen", "Small Tits" } },
            { "Little Asians", new[] { "Teen", "Asian" } },
            { "TeenJoi", new[] { "Teen", "JOI" } },
            { "Black Valley Girls", new[] { "Teen", "Ebony" } },
            { "Thickumz", new[] { "Thick" } },
            { "Dyked", new[] { "Hardcore", "Teen", "Lesbian" } },
            { "Teens Love Black Cocks", new[] { "Teen", "BBC" } },
        };

        public static async Task<JObject> GetJSONfromPage(string url, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await HTTP.Request(url, cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                var regEx = new Regex(@"window\.__INITIAL_STATE__ = (.*);").Match(http.Content);
                if (regEx.Groups.Count > 0)
                {
                    json = (JObject)JObject.Parse(regEx.Groups[1].Value)["content"];
                }
            }

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            if (!directURL.Contains("/", StringComparison.OrdinalIgnoreCase))
            {
                directURL = directURL.Replace("-", "/", 1, StringComparison.OrdinalIgnoreCase);
            }

            if (!int.TryParse(directURL.Split('/')[0], out _))
            {
                directURL = directURL.Replace("/", "-", 1, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                directURL = directURL.Split('/')[1];
            }

            directURL = Helper.GetSearchSearchURL(siteNum) + directURL;
            var searchResultsURLs = new List<string>
            {
                directURL,
            };

            var searchResults = await GoogleSearch.GetSearchResults(searchTitle, siteNum, cancellationToken).ConfigureAwait(false);
            foreach (var searchResult in searchResults)
            {
                var url = searchResult.Split('?').First();
                if (url.Contains("/movies/", StringComparison.OrdinalIgnoreCase) && !searchResultsURLs.Contains(url))
                {
                    searchResultsURLs.Add(url);
                }
            }

            foreach (var sceneURL in searchResultsURLs)
            {
                string curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}";
                if (searchDate.HasValue)
                {
                    curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
                }

                var sceneData = await this.Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(sceneData.Item.Name))
                {
                    sceneData.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                    var posters = (await this.GetImages(sceneData.Item, cancellationToken).ConfigureAwait(false)).Where(item => item.Type == ImageType.Primary);

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = sceneData.Item.ProviderIds,
                        Name = sceneData.Item.Name,
                        PremiereDate = sceneData.Item.PremiereDate,
                    };

                    if (posters.Any())
                    {
                        res.ImageUrl = posters.First().Url;
                    }

                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(string[] sceneID, CancellationToken cancellationToken)
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

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            string sceneURL = Helper.Decode(sceneID[2]),
                sceneDate = string.Empty;

            if (sceneID.Length > 3)
            {
                sceneDate = sceneID[3];
            }

            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            string contentName = string.Empty;
            foreach (var name in new List<string>() { "moviesContent", "videosContent" })
            {
                if (sceneData.ContainsKey(name) && sceneData[name].Any())
                {
                    contentName = name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(contentName))
            {
                return result;
            }

            sceneData = (JObject)sceneData[contentName];
            var sceneName = sceneData.Properties().First().Name;
            sceneData = (JObject)sceneData[sceneName];

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            switch (siteNum[0])
            {
                case 23:
                    result.Item.AddStudio("Mylf");
                    break;

                case 24:
                    result.Item.AddStudio("TeamSkeet");
                    break;
            }

            DateTime? releaseDate = null;
            if (sceneData.ContainsKey("publishedDate"))
            {
                releaseDate = (DateTime)sceneData["publishedDate"];
            }
            else
            {
                if (sceneID.Length > 3)
                {
                    if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                    {
                        releaseDate = sceneDateObj;
                    }
                }
            }

            if (releaseDate.HasValue)
            {
                result.Item.PremiereDate = releaseDate.Value;
            }

            string subSite;
            if (sceneData.ContainsKey("site"))
            {
                subSite = (string)sceneData["site"]["name"];
            }
            else
            {
                subSite = Helper.GetSearchSiteName(siteNum);
            }

            if (Genres.ContainsKey(subSite))
            {
                foreach (var genreName in Genres[subSite])
                {
                    result.Item.AddGenre(genreName);
                }
            }

            foreach (var genreName in new List<string>() { "MILF", "Mature" })
            {
                result.Item.AddGenre(genreName);
            }

            foreach (var actorLink in sceneData["models"])
            {
                string actorName = (string)actorLink["modelName"],
                       actorID = (string)actorLink["modelId"],
                       actorPhotoURL;

                var actorData = await GetJSONfromPage($"{Helper.GetSearchBaseURL(siteNum)}/models/{actorID}", cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorPhotoURL = (string)actorData["modelsContent"][actorID]["img"];
                    result.People.Add(new PersonInfo
                    {
                        Name = actorName,
                        ImageUrl = actorPhotoURL,
                    });
                }
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (item == null)
            {
                return result;
            }

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
            {
                return result;
            }

            var sceneID = externalId.Split('#');

            string sceneURL = Helper.Decode(sceneID[2]);

            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            string contentName = string.Empty;
            foreach (var name in new List<string>() { "moviesContent", "videosContent" })
            {
                if (sceneData.ContainsKey(name) && (sceneData[name] != null))
                {
                    contentName = name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(contentName))
            {
                return result;
            }

            sceneData = (JObject)sceneData[contentName];
            var sceneName = sceneData.Properties().First().Name;
            sceneData = (JObject)sceneData[sceneName];

            var img = (string)sceneData["img"];
            result.Add(new RemoteImageInfo
            {
                Url = img,
                Type = ImageType.Primary,
            });
            result.Add(new RemoteImageInfo
            {
                Url = img,
                Type = ImageType.Backdrop,
            });

            return result;
        }
    }
}
