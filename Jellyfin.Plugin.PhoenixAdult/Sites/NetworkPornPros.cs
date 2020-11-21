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
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class NetworkPornPros : IProviderBase
    {
        private static readonly Dictionary<string, string[]> Genres = new Dictionary<string, string[]>
        {
            { "Lubed", new[] { "Lube", "Raw", "Wet" } },
            { "Holed", new[] { "Anal", "Ass" } },
            { "POVD", new[] { "Gonzo", "Pov" } },
            { "MassageCreep", new[] { "Massage", "Oil" } },
            { "DeepThroatLove", new[] { "Blowjob", "Deep Throat" } },
            { "PureMature", new[] { "MILF", "Mature" } },
            { "Cum4K", new[] { "Creampie" } },
            { "GirlCum", new[] { "Orgasms", "Girl Orgasm", "Multiple Orgasms" } },
            { "PassionHD", new[] { "Hardcore" } },
            { "BBCPie", new[] { "Interracial", "BBC", "Creampie" } },
        };

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).Replace("'", "-", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(directURL.Substring(directURL.Length - 1, 1), out _) && directURL.Substring(directURL.Length - 2, 1) == "-")
            {
                directURL = $"{directURL.Substring(0, directURL.Length - 1)}-{directURL.Substring(directURL.Length - 1, 1)}";
            }

            string sceneURL = Helper.GetSearchSearchURL(siteNum) + directURL,
                curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}";
            if (searchDate.HasValue)
            {
                curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
            }

            string[] sceneID = curID.Split('#').Skip(2).ToArray();

            var sceneData = await this.Update(siteNum, sceneID, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(sceneData.Item.Name))
            {
                sceneData.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                var posters = (await this.GetImages(siteNum, sceneID, sceneData.Item, cancellationToken).ConfigureAwait(false)).Where(item => item.Type == ImageType.Primary);

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

            var sceneURL = Helper.Decode(sceneID[0]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@id, 'description')]");
            if (description != null)
            {
                result.Item.Overview = description.InnerText.Trim();
            }

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
                if (sceneID.Length > 1)
                {
                    sceneDate = sceneID[1];
                    dateFormat = "yyyy-MM-dd";
                }
            }

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var subSite = Helper.GetSearchSiteName(siteNum);
            if (Genres.ContainsKey(subSite))
            {
                foreach (var genreName in Genres[subSite])
                {
                    result.Item.AddGenre(genreName);
                }
            }

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'pt-md')]//a[contains(@href, '/girls/')]");
            if (actorsNode != null)
            {
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

                    result.People.Add(new PersonInfo
                    {
                        Name = actorName,
                    });
                }
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

            var sceneURL = Helper.Decode(sceneID[0]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var poster = sceneData.SelectSingleNode("//video[@id='player']");
            if (poster != null)
            {
                var img = poster.Attributes["poster"].Value;
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = $"https:{img}";
                }

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
            }

            return result;
        }
    }
}
