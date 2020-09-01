using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Providers.Helpers;

namespace PhoenixAdult.Providers.Sites
{
    internal class NetworkMylf : IPhoenixAdultProviderBase
    {
        public static async Task<JObject> GetJSONfromPage(string url, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await url.AllowAnyHttpStatus().GetAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
            {
                var data = await http.Content.ReadAsStringAsync().ConfigureAwait(false);
                var regEx = new Regex(@"window\.__INITIAL_STATE__ = (.*);").Match(data);
                if (regEx.Groups.Count > 0)
                    json = (JObject)JObject.Parse(regEx.Groups[1].Value)["content"];
            }

            return json;
        }

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null)
                return result;

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            if (!directURL.Contains("/", StringComparison.OrdinalIgnoreCase))
                directURL = directURL.Replace("-", "/", 1, StringComparison.OrdinalIgnoreCase);

            if (!int.TryParse(directURL.Split('/')[0], out _))
                directURL = directURL.Replace("/", "-", 1, StringComparison.OrdinalIgnoreCase);
            else
                directURL = directURL.Split('/')[1];

            directURL = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + directURL;
            var searchResultsURLs = new List<string>
            {
                directURL
            };

            var searchResults = await PhoenixAdultHelper.GetGoogleSearchResults(searchTitle, siteNum, cancellationToken).ConfigureAwait(false);
            foreach (var searchResult in searchResults)
            {
                var url = searchResult.Split('?').First();
                if (url.Contains("/movies/", StringComparison.OrdinalIgnoreCase) && !searchResultsURLs.Contains(url))
                    searchResultsURLs.Add(url);
            }

            foreach (var sceneURL in searchResultsURLs)
            {
                string curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}";
                if (searchDate.HasValue)
                    curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);
                if (sceneData != null)
                {
                    sceneData.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                    var posters = (await GetImages(sceneData.Item, cancellationToken).ConfigureAwait(false)).Where(item => item.Type == ImageType.Primary);

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = sceneData.Item.ProviderIds,
                        Name = sceneData.Item.Name,
                        PremiereDate = sceneData.Item.PremiereDate
                    };

                    if (posters.Any())
                        res.ImageUrl = posters.First().Url;

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
                People = new List<PersonInfo>()
            };

            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], CultureInfo.InvariantCulture), int.Parse(sceneID[1], CultureInfo.InvariantCulture) };

            string sceneURL = PhoenixAdultHelper.Decode(sceneID[2]),
                sceneDate = string.Empty;

            if (sceneID.Length > 3)
                sceneDate = sceneID[3];

            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return null;

            string contentName = string.Empty;
            foreach (var name in new List<string>() { "moviesContent", "videosContent" })
                if (sceneData.ContainsKey(name) && sceneData[name].Any())
                {
                    contentName = name;
                    break;
                }

            if (string.IsNullOrEmpty(contentName))
                return null;

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
                releaseDate = (DateTime)sceneData["publishedDate"];
            else
            {
                if (sceneID.Length > 3)
                    if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                        releaseDate = sceneDateObj;
            }

            if (releaseDate.HasValue)
                result.Item.PremiereDate = releaseDate.Value;

            string subSite;
            if (sceneData.ContainsKey("site"))
                subSite = (string)sceneData["site"]["name"];
            else
                subSite = PhoenixAdultHelper.GetSearchSiteName(siteNum);

            var genres = new List<string>();
            switch (subSite)
            {
                case "MylfBoss":
                    genres = new List<string> {
                        "Office", "Boss"
                    };
                    break;

                case "MylfBlows":
                    genres = new List<string> {
                        "Blowjob"
                    };
                    break;

                case "Milfty":
                    genres = new List<string> {
                        "Cheating"
                    };
                    break;

                case "Mom Drips":
                    genres = new List<string> {
                        "Creampie"
                    };
                    break;

                case "Milf Body":
                    genres = new List<string> {
                        "Gym", "Fitness"
                    };
                    break;

                case "Lone Milf":
                    genres = new List<string> {
                        "Solo"
                    };
                    break;

                case "Full Of JOI":
                    genres = new List<string> {
                        "JOI"
                    };
                    break;

                case "Mylfed":
                    genres = new List<string> {
                        "Lesbian"
                    };
                    break;

                case "MylfDom":
                    genres = new List<string> {
                        "BDSM"
                    };
                    break;

                case "Sis Loves Me":
                    genres = new List<string> {
                        "Step Sister"
                    };
                    break;

                case "DadCrush":
                case "DaughterSwap":
                    genres = new List<string> {
                        "Step Dad", "Step Daughter"
                    };
                    break;

                case "PervMom":
                    genres = new List<string> {
                        "Step Mom"
                    };
                    break;

                case "Family Strokes":
                    genres = new List<string> {
                        "Taboo Family"
                    };
                    break;

                case "Foster Tapes":
                    genres = new List<string> {
                        "Taboo Sex"
                    };
                    break;

                case "BFFs":
                    genres = new List<string> {
                        "Teen", "Group Sex"
                    };
                    break;

                case "Shoplyfter":
                    genres = new List<string> {
                        "Strip"
                    };
                    break;

                case "ShoplyfterMylf":
                    genres = new List<string> {
                        "Strip", "MILF"
                    };
                    break;

                case "Exxxtra Small":
                    genres = new List<string> {
                        "Teen", "Small Tits"
                    };
                    break;

                case "Little Asians":
                    genres = new List<string> {
                        "Teen", "Asian"
                    };
                    break;

                case "TeenJoi":
                    genres = new List<string> {
                        "Teen", "JOI"
                    };
                    break;

                case "Black Valley Girls":
                    genres = new List<string> {
                        "Teen", "Ebony"
                    };
                    break;

                case "Thickumz":
                    genres = new List<string> {
                        "Thick"
                    };
                    break;

                case "Dyked":
                    genres = new List<string> {
                        "Hardcore", "Teen", "Lesbian"
                    };
                    break;

                case "Teens Love Black Cocks":
                    genres = new List<string> {
                        "Teen", "BBC"
                    };
                    break;
            }

            foreach (var genreName in genres)
                result.Item.AddGenre(genreName);

            foreach (var genreName in new List<string>() { "MILF", "Mature" })
                result.Item.AddGenre(genreName);

            foreach (var actorLink in sceneData["models"])
            {
                string actorName = (string)actorLink["modelName"],
                       actorID = (string)actorLink["modelId"],
                       actorPhotoURL;

                var actorData = await GetJSONfromPage($"{PhoenixAdultHelper.GetSearchBaseURL(siteNum)}/models/{actorID}", cancellationToken).ConfigureAwait(false);
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

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            string sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);

            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return result;

            string contentName = string.Empty;
            foreach (var name in new List<string>() { "moviesContent", "videosContent" })
                if (sceneData.ContainsKey(name) && (sceneData[name] != null))
                {
                    contentName = name;
                    break;
                }

            if (string.IsNullOrEmpty(contentName))
                return result;

            sceneData = (JObject)sceneData[contentName];
            var sceneName = sceneData.Properties().First().Name;
            sceneData = (JObject)sceneData[sceneName];

            var img = (string)sceneData["img"];
            result.Add(new RemoteImageInfo
            {
                Url = img,
                Type = ImageType.Primary
            });
            result.Add(new RemoteImageInfo
            {
                Url = img,
                Type = ImageType.Backdrop
            });

            return result;
        }
    }
}
