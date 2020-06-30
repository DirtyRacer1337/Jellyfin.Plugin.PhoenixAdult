using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Sites
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
                directURL = directURL[0..^1] + "-" + directURL.Substring(directURL.Length - 1, 1);

            string sceneURL = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + directURL,
                    curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}";

            if (searchDate.HasValue)
            {
                var date = searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultHelper.Lang);
                curID += $"#{date}";
            }

            var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);

            result.Add(new RemoteSearchResult
            {
                ProviderIds = { { PhoenixAdultProvider.PluginName, curID } },
                Name = sceneData.Item.Name
            });

            return result;
        }

        public async Task<MetadataResult<Movie>> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie()
            };
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultHelper.Lang), int.Parse(sceneID[1], PhoenixAdultHelper.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            result.Item.Name = sceneData.SelectSingleNode("//h1").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@id, 'description')]");
            if (description != null)
                result.Item.Overview = description.InnerText.Trim();
            result.Item.AddStudio("Porn Pros");

            var dateNode = sceneData.SelectSingleNode("//div[@class='d-inline d-lg-block mb-1']/span");
            string date = null, dateFormat = null;
            if (dateNode != null)
            {
                date = dateNode.InnerText.Trim();
                dateFormat = "MMMM dd, yyyy";
            }
            else
            {
                if (sceneID.Length > 3)
                {
                    date = sceneID[3];
                    dateFormat = "yyyy-MM-dd";
                }
            }
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(dateFormat))
                if (DateTime.TryParseExact(date, dateFormat, PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                    result.Item.ProductionYear = sceneDateObj.Year;
                }

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

                    result.AddPerson(new PersonInfo
                    {
                        Name = actorName,
                        Type = PersonType.Actor
                    });
                }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();
            if (item == null)
                return images;

            string[] sceneID = item.ProviderIds[PhoenixAdultProvider.PluginName].Split('#');

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var http = await sceneURL.GetAsync(cancellationToken).ConfigureAwait(false);
            var html = new HtmlDocument();
            html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var sceneData = html.DocumentNode;

            var poster = sceneData.SelectSingleNode("//video[@id='player']");
            if (poster != null)
            {
                var img = poster.Attributes["poster"].Value;
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    img = $"https:{img}";
                images.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Primary,
                    ProviderName = PhoenixAdultProvider.PluginName
                });
                images.Add(new RemoteImageInfo
                {
                    Url = img,
                    Type = ImageType.Backdrop,
                    ProviderName = PhoenixAdultProvider.PluginName
                });
            }

            return images;
        }
    }
}
