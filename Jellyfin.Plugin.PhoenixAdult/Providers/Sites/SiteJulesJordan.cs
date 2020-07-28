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
using PhoenixAdult.Providers.Helpers;

namespace PhoenixAdult.Providers.Sites
{
    internal class SiteJulesJordan : IPhoenixAdultProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var url = PhoenixAdultHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//div[@class='update_details']");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = searchResult.SelectSingleNode("./a[last()]").Attributes["href"].Value,
                        curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultHelper.Encode(sceneURL)}",
                        sceneName = searchResult.SelectSingleNode("./a[last()]").InnerText.Trim(),
                        scenePoster = searchResult.SelectSingleNode(".//img[1]").Attributes["src"].Value;
                var sceneDateNode = searchResult.SelectSingleNode(".//div[contains(@class, 'update_date')]");

                var res = new RemoteSearchResult
                {
                    Name = sceneName,
                    ImageUrl = scenePoster
                };

                if (sceneDateNode != null)
                {
                    var sceneDate = sceneDateNode.InnerText.Trim();
                    if (string.IsNullOrEmpty(sceneDate))
                    {
                        sceneDate = sceneDateNode.InnerHtml.Trim();
                        if (sceneDate.Contains("<!--", StringComparison.OrdinalIgnoreCase))
                            sceneDate = sceneDate.Replace("<!--", "", StringComparison.OrdinalIgnoreCase).Replace("-->", "", StringComparison.OrdinalIgnoreCase).Replace("Date OFF", "", StringComparison.OrdinalIgnoreCase).Trim();
                    }

                    if (DateTime.TryParseExact(sceneDate, "MM/dd/yyyy", PhoenixAdultProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    {
                        res.PremiereDate = sceneDateObj;
                        curID += $"#{sceneDateObj.ToString("yyyy-MM-dd", PhoenixAdultProvider.Lang)}";
                    }
                }

                res.ProviderIds.Add(Plugin.Instance.Name, curID);

                result.Add(res);
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
                return result;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            string sceneURL = PhoenixAdultHelper.Decode(sceneID[2]),
                sceneDate = sceneID[3];
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Item.Name = sceneData.SelectSingleNode("//span[@class='title_bar_hilite']").InnerText.Trim();
            result.Item.Overview = sceneData.SelectSingleNode("//span[@class='update_description']").InnerText.Trim();
            result.Item.AddStudio("Jules Jordan");

            if (!string.IsNullOrEmpty(sceneDate))
                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", PhoenixAdultProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.Item.PremiereDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("//span[@class='update_tags']/a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Item.AddGenre(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@class='gallery_info']/*[1]//span[@class='update_models']//a");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    var actor = new PersonInfo
                    {
                        Name = actorLink.InnerText.Trim()
                    };

                    var actorPage = await HTML.ElementFromURL(actorLink.Attributes["href"].Value, cancellationToken).ConfigureAwait(false);
                    var actorPhotoNode = actorPage.SelectSingleNode("//img[contains(@class, 'model_bio_thumb')]");
                    if (actorPhotoNode != null)
                    {
                        string actorPhoto;
                        if (actorPhotoNode.Attributes.Contains("src0_3x"))
                            actorPhoto = actorPhotoNode.Attributes["src0_3x"].Value;
                        else
                            actorPhoto = actorPhotoNode.Attributes["src0"].Value;

                        if (!actorPhoto.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            actorPhoto = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + actorPhoto;

                        actor.ImageUrl = actorPhoto;
                    }

                    result.People.Add(actor);
                }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (!item.ProviderIds.TryGetValue(Plugin.Instance.Name, out string externalId))
                return result;

            var sceneID = externalId.Split('#');

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultProvider.Lang), int.Parse(sceneID[1], PhoenixAdultProvider.Lang) };

            var sceneURL = PhoenixAdultHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            var script = sceneData.SelectSingleNode("//script[contains(text(), 'df_movie')]").InnerText;
            var match = Regex.Match(script, "useimage = \"(.*)\";");
            if (match.Success)
                result.Add(new RemoteImageInfo
                {
                    Url = $"https:{match.Groups[1].Value}",
                    Type = ImageType.Primary
                });

            match = Regex.Match(script, "setid:.?\"(\\d{1,})\"");
            if (match.Success)
            {
                var setId = match.Groups[1].Value;
                var sceneSearch = await HTML.ElementFromURL(PhoenixAdultHelper.GetSearchSearchURL(siteNum) + Uri.EscapeDataString(item.Name), cancellationToken).ConfigureAwait(false);

                var scenePosters = sceneSearch.SelectSingleNode($"//img[@id='set-target-{setId}' and contains(@class, 'hideMobile')]");
                if (scenePosters != null)
                    for (int i = 0; i <= scenePosters.Attributes.Count(o => o.Name.Contains("src", StringComparison.OrdinalIgnoreCase)); i++)
                    {
                        var attrName = $"src{i}_1x";
                        if (scenePosters.Attributes.Contains(attrName))
                        {
                            var img = scenePosters.Attributes[attrName].Value;
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
                    }
            }

            var photoPageURL = sceneData.SelectSingleNode("//div[contains(@class, 'content_tab')]/a[text()='Photos']").Attributes["href"].Value;
            var photoPage = await HTML.ElementFromURL(photoPageURL, cancellationToken).ConfigureAwait(false);
            script = photoPage.SelectSingleNode("//script[contains(text(), 'ptx[\"1600\"]')]").InnerText;
            var matches = Regex.Matches(script, "ptx\\[\"1600\"\\].*{src:.?\"(.*?)\"");
            if (matches.Count > 0)
                for (int i = 1; i <= 10; i++)
                {
                    var t = (matches.Count / 10 * i) - 1;
                    var img = matches[t].Groups[1].Value;
                    if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        img = PhoenixAdultHelper.GetSearchBaseURL(siteNum) + img;

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
