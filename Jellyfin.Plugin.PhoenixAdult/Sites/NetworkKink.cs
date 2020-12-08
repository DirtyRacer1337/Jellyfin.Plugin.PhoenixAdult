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
    public class NetworkKink : IProviderBase
    {
        private readonly IDictionary<string, string> cookies = new Dictionary<string, string>
        {
            { "viewing-preferences", "straight%2Cgay" },
        };

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var splitedTitle = searchTitle.Split()[0];
            if (int.TryParse(splitedTitle, out _))
            {
                string sceneURL = $"{Helper.GetSearchBaseURL(siteNum)}/shoot/{splitedTitle}",
                    curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}";
                var sceneID = curID.Split('#').Skip(2).ToArray();

                var searchResult = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID, cancellationToken).ConfigureAwait(false);
                if (searchResult.Any())
                {
                    result.AddRange(searchResult);
                }
            }
            else
            {
                var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
                var data = await HTML.ElementFromURL(url, cancellationToken, null, this.cookies).ConfigureAwait(false);

                var searchResults = data.SelectNodes("//div[@class='shoot-card scene']");
                if (searchResults != null)
                {
                    foreach (var searchResult in searchResults)
                    {
                        string sceneURL = Helper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a[@class='shoot-link']").Attributes["href"].Value,
                                curID = $"{siteNum[0]}#{siteNum[1]}#{Helper.Encode(sceneURL)}",
                                sceneName = searchResult.SelectSingleNode(".//img").Attributes["alt"].Value,
                                scenePoster = searchResult.SelectSingleNode(".//img").Attributes["src"].Value,
                                sceneDate = searchResult.SelectSingleNode(".//div[@class='date']").InnerText.Trim();

                        var res = new RemoteSearchResult
                        {
                            ProviderIds = { { Plugin.Instance.Name, curID } },
                            Name = sceneName,
                            ImageUrl = scenePoster,
                        };

                        if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
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
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken, null, this.cookies).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleNode("//h1[@class='shoot-title']").GetDirectInnerText();
            result.Item.Overview = sceneData.SelectSingleNode("//*[@class='description-text']").InnerText;
            result.Item.AddStudio("Kink");

            var sceneDate = sceneData.SelectSingleNode("//span[@class='shoot-date']").InnerText.Trim();
            if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var genres = sceneData.SelectNodes("//p[@class='tag-list category-tag-list']//a");
            if (genres != null)
            {
                foreach (var genreLink in genres)
                {
                    var genreName = genreLink.InnerText;

                    result.Item.AddGenre(genreName);
                }
            }

            var actors = sceneData.SelectNodes("//p[@class='starring']//a");
            if (actors != null)
            {
                foreach (var actorLink in actors)
                {
                    string actorName = actorLink.InnerText.Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase),
                           actorPageURL = Helper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                           actorPhoto = string.Empty;

                    var res = new PersonInfo
                    {
                        Name = actorName,
                    };

                    var actorHTML = await HTML.ElementFromURL(actorPageURL, cancellationToken, null, this.cookies).ConfigureAwait(false);
                    var actorPhotoNode = actorHTML.SelectSingleNode("//div[contains(@class, 'biography-container')]//img");
                    if (actorPhotoNode != null)
                    {
                        actorPhoto = actorPhotoNode.Attributes["src"].Value;
                    }

                    if (!string.IsNullOrEmpty(actorPhoto))
                    {
                        res.ImageUrl = actorPhoto;
                    }

                    result.People.Add(res);
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
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken, null, this.cookies).ConfigureAwait(false);

            var sceneImages = sceneData.SelectNodes("//video");
            if (sceneImages != null)
            {
                foreach (var sceneImage in sceneImages)
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["poster"].Value,
                        Type = ImageType.Primary,
                    });
                }
            }

            sceneImages = sceneData.SelectNodes("//div[@class='player']//img");
            if (sceneImages != null)
            {
                foreach (var sceneImage in sceneImages)
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["src"].Value,
                        Type = ImageType.Primary,
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["src"].Value,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            sceneImages = sceneData.SelectNodes("//div[@id='gallerySlider']//img");
            if (sceneImages != null)
            {
                foreach (var sceneImage in sceneImages)
                {
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["data-image-file"].Value,
                        Type = ImageType.Primary,
                    });
                    result.Add(new RemoteImageInfo
                    {
                        Url = sceneImage.Attributes["data-image-file"].Value,
                        Type = ImageType.Backdrop,
                    });
                }
            }

            return result;
        }
    }
}
