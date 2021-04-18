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
    public class NetworkCaribbeancom : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var splitedSearchTitle = searchTitle.Split();
            var movieID = string.Empty;
            if (int.TryParse(splitedSearchTitle[0], out _) && int.TryParse(splitedSearchTitle[1], out _))
            {
                var separator = string.Empty;
                switch (siteNum[1])
                {
                    case 0:
                        separator = "-";
                        break;
                    case 1:
                        separator = "_";
                        break;
                }

                if (!string.IsNullOrEmpty(separator))
                {
                    movieID = splitedSearchTitle[0] + separator + splitedSearchTitle[1];
                }
            }

            if (!string.IsNullOrEmpty(movieID))
            {
                var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + $"/eng/moviepages/{movieID}/index.html");
                var sceneID = new string[] { Helper.Encode(sceneURL.AbsolutePath) };

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, searchDate, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);
                }
            }
            else
            {
                var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodesSafe("//div[contains(@class, 'list') or contains(@class, 'is-movie')]//div[@class='grid-item']");
                foreach (var searchResult in searchResults)
                {
                    var sceneURL = new Uri(Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//div[@class='meta-title']/a/@href"));
                    string curID = Helper.Encode(sceneURL.AbsolutePath),
                        sceneName = searchResult.SelectSingleText(".//div[@class='meta-title']"),
                        sceneDate = searchResult.SelectSingleText(".//div[@class='meta-data']"),
                        scenePoster = Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleText(".//div[@class='media-thum']//img/@src");

                    var res = new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneName,
                        ImageUrl = scenePoster,
                    };

                    if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                    }

                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//div[contains(@class, 'heading')]//h1");
            result.Item.Overview = sceneData.SelectSingleText("//p[@itemprop='description']");

            var movieSpecNodes = sceneData.SelectNodesSafe("//li[@class='movie-spec' or @class='movie-detail__spec']");
            foreach (var movieSpec in movieSpecNodes)
            {
                var movieSpecTitle = movieSpec.SelectSingleText(".//span[@class='spec-title']").Replace(":", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                switch (movieSpecTitle)
                {
                    case "Release Date":
                        var date = movieSpec.SelectSingleText(".//span[@class='spec-content']").Replace("/", "-", StringComparison.OrdinalIgnoreCase);
                        if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                        {
                            result.Item.PremiereDate = sceneDateObj;
                        }

                        break;

                    case "Tags":
                        var genreNode = movieSpec.SelectNodesSafe(".//span[@class='spec-content']/a");
                        foreach (var genreLink in genreNode)
                        {
                            var genreName = genreLink.InnerText;

                            result.Item.AddGenre(genreName);
                        }

                        break;

                    case "Starring":
                        var actorsNode = movieSpec.SelectNodesSafe(".//span[@class='spec-content']/a");
                        foreach (var actorLink in actorsNode)
                        {
                            var actorName = actorLink.InnerText;

                            switch (Plugin.Instance.Configuration.JAVActorNamingStyle)
                            {
                                case JAVActorNamingStyle.JapaneseStyle:
                                    actorName = string.Join(" ", actorName.Split().Reverse());

                                    break;
                            }

                            var actor = new PersonInfo
                            {
                                Name = actorName,
                            };

                            result.People.Add(actor);
                        }

                        break;
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
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);
            var movieID = sceneURL.Replace("/index.html", string.Empty, StringComparison.OrdinalIgnoreCase).Split("/").Last();

            result.Add(new RemoteImageInfo
            {
                Url = Helper.GetSearchBaseURL(siteNum) + $"/moviepages/{movieID}/images/l.jpg",
                Type = ImageType.Primary,
            });
            result.Add(new RemoteImageInfo
            {
                Url = Helper.GetSearchBaseURL(siteNum) + $"/moviepages/{movieID}/images/l_l.jpg",
                Type = ImageType.Primary,
            });

            var sceneImages = sceneData.SelectNodesSafe("//div[@class='gallery' or contains(@class, 'is-gallery')]//a");
            foreach (var sceneImage in sceneImages)
            {
                var img = sceneImage.Attributes["href"].Value;

                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = Helper.GetSearchBaseURL(siteNum) + img;
                }

                if (!img.Contains("/member/", StringComparison.OrdinalIgnoreCase))
                {
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
            }

            return result;
        }
    }
}
