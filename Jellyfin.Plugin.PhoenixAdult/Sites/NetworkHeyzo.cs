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
using PhoenixAdult.Configuration;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class NetworkHeyzo : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var splitedSearchTitle = searchTitle.Split();
            string movieID = string.Empty;
            if (int.TryParse(splitedSearchTitle[0], out _))
            {
                movieID = splitedSearchTitle[0];
            }

            if (!string.IsNullOrEmpty(movieID))
            {
                string sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/moviepages/{movieID}/index.html",
                    curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}";
                string[] sceneID = curID.Split('#').Skip(2).ToArray();

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);
                }
            }
            else
            {
                var url = string.Format(CultureInfo.InvariantCulture, Helper.GetSearchSearchURL(siteNum), searchTitle);
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodes("//div[@id='movies']//div[contains(@class, 'movie')]");
                if (searchResults != null)
                {
                    foreach (var searchResult in searchResults)
                    {
                        string sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/en/?v={searchResult.SelectSingleText(".//a/@href")}",
                            curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleText(".//a[@class='actor']"),
                            sceneDate = searchResult.SelectSingleText(".//p[@class='release']").Replace("Release:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(),
                            scenePoster = Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//img/@data-original");

                        var res = new RemoteSearchResult
                        {
                            ProviderIds = { { Plugin.Instance.Name, curID } },
                            Name = sceneName,
                            ImageUrl = scenePoster,
                        };

                        if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                        {
                            res.PremiereDate = sceneDateObj;
                        }

                        result.Add(res);
                    }
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

            var sceneURL = Helper.Decode(sceneID[0]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.HomePageUrl = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//h1");

            result.Item.AddStudio("Heyzo");

            foreach (var movieInfo in sceneData.SelectNodes("//table[@class='movieInfo']//tr"))
            {
                var cellNode = movieInfo.SelectNodes("./td");
                switch (cellNode[0].InnerText)
                {
                    case "Released":
                        var date = cellNode[1].InnerText.Trim();
                        if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                        {
                            result.Item.PremiereDate = sceneDateObj;
                        }

                        break;
                    case "Sex Styles":
                        var genreNode = cellNode[1].SelectNodes(".//a[contains(@href, 'category')]");
                        if (genreNode != null)
                        {
                            foreach (var genreLink in genreNode)
                            {
                                var genreName = genreLink.InnerText;

                                result.Item.AddGenre(genreName);
                            }
                        }

                        break;
                    case "Actress(es)":
                        var actorsNode = cellNode[1].SelectNodes(".//a[contains(@href, 'actor')]");
                        if (actorsNode != null)
                        {
                            foreach (var actorLink in actorsNode)
                            {
                                string actorName = actorLink.InnerText;

                                if (Plugin.Instance.Configuration.JAVActorNamingStyle == JAVActorNamingStyle.JapaneseStyle)
                                {
                                    actorName = string.Join(" ", actorName.Split().Reverse());
                                }

                                var actor = new PersonInfo
                                {
                                    Name = actorName,
                                };

                                result.People.Add(actor);
                            }
                        }

                        break;
                }
            }

            return result;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var sceneURL = Helper.Decode(sceneID[0]);

            var movieID = sceneURL.Replace("/index.html", string.Empty, StringComparison.OrdinalIgnoreCase).Split("/").Last();
            if (!string.IsNullOrEmpty(movieID) && int.TryParse(movieID, out _))
            {
                result.Add(new RemoteImageInfo
                {
                    Url = $"https://en.heyzo.com/contents/3000/{movieID}/images/player_thumbnail_en.jpg",
                    Type = ImageType.Primary,
                });
            }

            for (var i = 0; i <= 21; i++)
            {
                string index = i.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0'),
                    img = $"https://en.heyzo.com/contents/3000/{movieID}/gallery/{index}.jpg";

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
