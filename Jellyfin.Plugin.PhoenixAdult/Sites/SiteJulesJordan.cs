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
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class SiteJulesJordan : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[@class='update_details']");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(searchResult.SelectSingleText("./a[last()]/@href"));
                string curID = Helper.Encode(sceneURL.AbsolutePath),
                    sceneName = searchResult.SelectSingleText("./a[last()]"),
                    scenePoster = searchResult.SelectSingleText(".//img[1]/@src");
                var sceneDateNode = searchResult.SelectSingleNode(".//div[contains(@class, 'update_date')]");

                var res = new RemoteSearchResult
                {
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (sceneDateNode != null)
                {
                    var sceneDate = sceneDateNode.InnerText.Trim();
                    if (string.IsNullOrEmpty(sceneDate))
                    {
                        sceneDate = sceneDateNode.InnerHtml.Trim();
                        if (sceneDate.Contains("<!--", StringComparison.OrdinalIgnoreCase))
                        {
                            sceneDate = sceneDate
                                .Replace("<!--", string.Empty, StringComparison.OrdinalIgnoreCase)
                                .Replace("-->", string.Empty, StringComparison.OrdinalIgnoreCase)
                                .Replace("Date OFF", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                        }
                    }

                    if (DateTime.TryParseExact(sceneDate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                        curID += $"#{sceneDateObj.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
                    }
                }

                res.ProviderIds.Add(Plugin.Instance.Name, curID);

                result.Add(res);
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

            string sceneURL = Helper.Decode(sceneID[0]),
                sceneDate = sceneID[1];

            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.ExternalId = sceneURL;

            result.Item.Name = sceneData.SelectSingleText("//span[@class='title_bar_hilite']");
            result.Item.Overview = sceneData.SelectSingleText("//span[@class='update_description']");
            result.Item.AddStudio("Jules Jordan");

            if (!string.IsNullOrEmpty(sceneDate))
            {
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.Item.PremiereDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodesSafe("//span[@class='update_tags']/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[@class='gallery_info']/*[1]//span[@class='update_models']//a");
            foreach (var actorLink in actorsNode)
            {
                var actor = new PersonInfo
                {
                    Name = actorLink.InnerText,
                };

                var actorPage = await HTML.ElementFromURL(actorLink.Attributes["href"].Value, cancellationToken).ConfigureAwait(false);
                var actorPhotoNode = actorPage.SelectSingleNode("//img[contains(@class, 'model_bio_thumb')]");
                if (actorPhotoNode != null)
                {
                    string actorPhoto = string.Empty;
                    if (actorPhotoNode.Attributes.Contains("src0_3x"))
                    {
                        actorPhoto = actorPhotoNode.Attributes["src0_3x"].Value;
                    }
                    else
                    {
                        if (actorPhotoNode.Attributes.Contains("src0"))
                        {
                            actorPhoto = actorPhotoNode.Attributes["src0"].Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(actorPhoto))
                    {
                        if (!actorPhoto.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            actorPhoto = Helper.GetSearchBaseURL(siteNum) + actorPhoto;
                        }

                        actor.ImageUrl = actorPhoto;
                    }
                }

                result.People.Add(actor);
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null || string.IsNullOrEmpty(item?.Name))
            {
                return result;
            }

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var script = sceneData.SelectSingleText("//script[contains(text(), 'df_movie')]");
            if (!string.IsNullOrEmpty(script))
            {
                var match = Regex.Match(script, "useimage = \"(.*)\";");
                if (match.Success)
                {
                    var img = match.Groups[1].Value;
                    if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        img = Helper.GetSearchBaseURL(siteNum) + img;
                    }

                    result.Add(new RemoteImageInfo
                    {
                        Url = img,
                        Type = ImageType.Primary,
                    });
                }

                match = Regex.Match(script, "setid:.?\"([0-9]{1,})\"");
                if (match.Success)
                {
                    var setId = match.Groups[1].Value;
                    var sceneSearch = await HTML.ElementFromURL(Helper.GetSearchSearchURL(siteNum) + Uri.EscapeDataString(item.Name), cancellationToken).ConfigureAwait(false);

                    var scenePosters = sceneSearch.SelectSingleNode($"//img[@id='set-target-{setId}' and contains(@class, 'hideMobile')]");
                    if (scenePosters != null)
                    {
                        for (var i = 0; i <= scenePosters.Attributes.Count(o => o.Name.Contains("src", StringComparison.OrdinalIgnoreCase)); i++)
                        {
                            var attrName = $"src{i}_1x";
                            if (scenePosters.Attributes.Contains(attrName))
                            {
                                var img = scenePosters.Attributes[attrName].Value;
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
                    }
                }
            }

            var photoPageURL = sceneData.SelectSingleText("//div[contains(@class, 'content_tab')]/a[text()='Photos']/@href");
            var photoPage = await HTML.ElementFromURL(photoPageURL, cancellationToken).ConfigureAwait(false);
            script = photoPage.SelectSingleText("//script[contains(text(), 'ptx[\"1600\"]')]");
            if (!string.IsNullOrEmpty(script))
            {
                var matches = Regex.Matches(script, "ptx\\[\"1600\"\\].*{src:.?\"(.*?)\"");
                if (matches.Count > 0)
                {
                    for (var i = 1; i <= 10; i++)
                    {
                        var t = (matches.Count / 10 * i) - 1;
                        var img = matches[t].Groups[1].Value;
                        if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            img = Helper.GetSearchBaseURL(siteNum) + img;
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
                }
            }

            return result;
        }
    }
}
