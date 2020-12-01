using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class NetworkNubiles : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            if (searchDate.HasValue)
            {
                var date = searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var url = $"{Helper.GetSearchSearchURL(siteNum)}/date/{date}/{date}";
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodes("//div[contains(@class, 'content-grid-item')]");
                if (searchResults != null)
                {
                    foreach (var searchResult in searchResults)
                    {
                        string sceneID = searchResult.SelectSingleNode(".//span[@class='title']/a").Attributes["href"].Value.Split('/')[3],
                            curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}",
                            sceneName = searchResult.SelectSingleNode(".//span[@class='title']/a | //h2").InnerText,
                            posterURL = searchResult.SelectSingleNode(".//noscript/picture/img").Attributes["src"].Value,
                            sceneDate = searchResult.SelectSingleNode(".//span[@class='date']").InnerText;

                        var res = new RemoteSearchResult
                        {
                            ProviderIds = { { Plugin.Instance.Name, curID } },
                            Name = sceneName,
                            ImageUrl = posterURL,
                        };

                        if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                        {
                            res.PremiereDate = sceneDateObj;
                        }

                        result.Add(res);
                    }
                }
            }
            else
            {
                if (int.TryParse(searchTitle.Split()[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sceneID))
                {
                    var sceneURL = $"{Helper.GetSearchSearchURL(siteNum)}watch/{sceneID}";
                    var data = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

                    var sceneData = data.SelectSingleNode("//div[contains(@class, 'content-pane-title')]");
                    if (sceneData != null)
                    {
                        string curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID.ToString(CultureInfo.InvariantCulture)}",
                            sceneName = sceneData.SelectSingleNode("//h2").InnerText,
                            posterURL = sceneData.SelectSingleNode("//video").Attributes["poster"].Value,
                            sceneDate = sceneData.SelectSingleNode("//span[@class='date']").InnerText;

                        var res = new RemoteSearchResult
                        {
                            ProviderIds = { { Plugin.Instance.Name, curID } },
                            Name = sceneName,
                            ImageUrl = posterURL,
                        };

                        if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
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

            string sceneURL = sceneID[0];

            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchSearchURL(siteNum) + $"watch/{sceneID[0]}";
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.HomePageUrl = sceneURL;

            result.Item.Name = sceneData.SelectSingleNode("//div[contains(@class, 'content-pane-title')]//h2").InnerText;
            var description = sceneData.SelectSingleNode("//div[@class='col-12 content-pane-column']/div");
            if (description == null)
            {
                var desc = string.Empty;
                var paragraphs = sceneData.SelectNodes("//div[@class='col-12 content-pane-column']//p");
                if (paragraphs != null)
                {
                    foreach (var paragraph in paragraphs)
                    {
                        desc += "\n\n" + paragraph.InnerText.Trim();
                    }

                    result.Item.Overview = desc;
                }
            }
            else
            {
                result.Item.Overview = description.InnerText;
            }

            result.Item.AddStudio("Nubiles");

            var sceneDate = sceneData.SelectSingleNode("//div[contains(@class, 'content-pane')]//span[@class='date']");
            if (sceneDate != null)
            {
                if (DateTime.TryParseExact(sceneDate.InnerText.Trim(), "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodes("//div[@class='categories']/a");
            if (genreNode != null)
            {
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Item.AddGenre(genreName);
                }
            }

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'content-pane-performer')]/a");
            if (actorsNode != null)
            {
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText,
                        actorPageURL = Helper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value;

                    var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);
                    var actorPhotoURL = actorPage.SelectSingleNode("//div[contains(@class, 'model-profile')]//img").Attributes["src"].Value;

                    result.People.Add(new PersonInfo
                    {
                        Name = actorName,
                        ImageUrl = actorPhotoURL,
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

            string sceneURL = sceneID[0];
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchSearchURL(siteNum) + $"watch/{sceneID[0]}";
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var poster = sceneData.SelectSingleNode("//video");
            if (poster != null)
            {
                result.Add(new RemoteImageInfo
                {
                    Url = poster.Attributes["poster"].Value,
                    Type = ImageType.Primary,
                });
            }

            var photoPageURL = "https://nubiles-porn.com/photo/gallery/" + sceneID[0];
            var photoPage = await HTML.ElementFromURL(photoPageURL, cancellationToken).ConfigureAwait(false);
            var sceneImages = photoPage.SelectNodes("//div[@class='img-wrapper']//source[1]");
            if (sceneImages != null)
            {
                foreach (var sceneImage in sceneImages)
                {
                    var posterURL = sceneImage.Attributes["src"].Value;

                    result.Add(new RemoteImageInfo
                    {
                        Url = posterURL,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            return result;
        }
    }
}
