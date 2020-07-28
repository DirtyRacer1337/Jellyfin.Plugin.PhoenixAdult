using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Providers.Helpers;

namespace PhoenixAdult.Providers.Sites
{
    internal class NetworkPornPros : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).Replace("'", "-", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(directURL.Substring(directURL.Length - 1, 1), out _) && directURL.Substring(directURL.Length - 2, 1) == "-")
                directURL = $"{directURL.Substring(0, directURL.Length - 1)}-{directURL.Substring(directURL.Length - 1, 1)}";

            string sceneURL = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + directURL,
                    curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}";

            if (searchDate.HasValue)
                curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultProvider.Lang)}";

            var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);
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
                return result;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@id, 'description')]");
            if (description != null)
                result.Item.Overview = description.InnerText.Trim();
            result.Item.AddStudio("Porn Pros");

            var dateNode = sceneData.SelectSingleNode("//div[@class='d-inline d-lg-block mb-1']/span");
            string sceneDate = string.Empty, dateFormat = string.Empty;
            if (dateNode != null)
            {
                sceneDate = dateNode.InnerText.Trim();
                dateFormat = "MMMM dd, yyyy";
            }
            else
            {
                if (sceneID.Length > 3)
                {
                    sceneDate = sceneID[3];
                    dateFormat = "yyyy-MM-dd";
                }
            }
            if (!string.IsNullOrEmpty(sceneDate) && !string.IsNullOrEmpty(dateFormat))
                if (DateTime.TryParseExact(sceneDate, dateFormat, PhoenixAdultProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.Item.PremiereDate = sceneDateObj;

            var genres = new List<string>();
            switch (PhoenixAdultHelper.GetSearchSiteName(siteNum))
            {
                case "Lubed":
                    genres = new List<string> {
                        "Lube", "Raw", "Wet"
                    };
                    break;

                case "Holed":
                    genres = new List<string> {
                        "Anal", "Ass"
                    };
                    break;

                case "POVD":
                    genres = new List<string> {
                        "Gonzo", "Pov"
                    };
                    break;

                case "MassageCreep":
                    genres = new List<string> {
                        "Massage", "Oil"
                    };
                    break;

                case "DeepThroatLove":
                    genres = new List<string> {
                        "Blowjob", "Deep Throat"
                    };
                    break;

                case "PureMature":
                    genres = new List<string> {
                        "MILF", "Mature"
                    };
                    break;

                case "Cum4K":
                    genres = new List<string> {
                        "Creampie"
                    };
                    break;

                case "GirlCum":
                    genres = new List<string> {
                        "Orgasms", "Girl Orgasm", "Multiple Orgasms"
                    };
                    break;

                case "PassionHD":
                    genres = new List<string> {
                        "Hardcore"
                    };
                    break;

                case "BBCPie":
                    genres = new List<string> {
                        "Interracial", "BBC", "Creampie"
                    };
                    break;
            }

            foreach (var genreName in genres)
                result.Item.AddGenre(genreName);

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'pt-md')]//a[contains(@href, '/girls/')]");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

                    result.People.Add(new PersonInfo
                    {
                        Name = actorName
                    });
                }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var poster = sceneData.SelectSingleNode("//video[@id='player']");
            if (poster != null)
            {
                var img = poster.Attributes["poster"].Value;
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    img = $"https:{img}";

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
            }

            return result;
        }
    }
}
