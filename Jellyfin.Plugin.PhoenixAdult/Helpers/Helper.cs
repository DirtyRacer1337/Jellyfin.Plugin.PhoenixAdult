using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Helpers
{
    internal static class Helper
    {
        public static string GetSearchSiteName(int[] siteNum)
        {
            if (siteNum == null)
            {
                return string.Empty;
            }

            return Database.SiteList.Sites[siteNum[0]][siteNum[1]][0];
        }

        public static string GetSearchBaseURL(int[] siteNum)
        {
            if (siteNum == null)
            {
                return string.Empty;
            }

            string url;
            if (!string.IsNullOrEmpty(Database.SiteList.Sites[siteNum[0]][siteNum[1]].ElementAtOrDefault(1)))
            {
                url = Database.SiteList.Sites[siteNum[0]][siteNum[1]][1];
            }
            else
            {
                url = Database.SiteList.Sites[siteNum[0]].First().Value[1];
            }

            return url;
        }

        public static string GetSearchSearchURL(int[] siteNum)
        {
            if (siteNum == null)
            {
                return string.Empty;
            }

            string url;
            if (!string.IsNullOrEmpty(Database.SiteList.Sites[siteNum[0]][siteNum[1]].ElementAtOrDefault(2)))
            {
                url = Database.SiteList.Sites[siteNum[0]][siteNum[1]][2];
            }
            else
            {
                url = Database.SiteList.Sites[siteNum[0]].First().Value[2];
            }

            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = GetSearchBaseURL(siteNum) + url;
            }

            return url;
        }

        public static string Encode(string text)
            => Base58.EncodePlain(Encoding.UTF8.GetBytes(text));

        public static string Decode(string base64Text)
            => Encoding.UTF8.GetString(Base58.DecodePlain(base64Text));

        public static KeyValuePair<int[], string> GetSiteFromTitle(string title)
        {
            var clearName = Regex.Replace(title, @"\W", string.Empty);
            var possibleSites = new Dictionary<int[], string>();

            foreach (var site in Database.SiteList.Sites)
            {
                foreach (var siteData in site.Value)
                {
                    var clearSite = Regex.Replace(siteData.Value[0], @"\W", string.Empty);
                    if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                    {
                        possibleSites.Add(new int[] { site.Key, siteData.Key }, clearSite);
                    }
                }
            }

            if (possibleSites.Count > 0)
            {
                return possibleSites.OrderByDescending(x => x.Value.Length).First();
            }

            return new KeyValuePair<int[], string>(null, null);
        }

        public static string GetClearTitle(string title, string siteName)
        {
            if (string.IsNullOrEmpty(title))
            {
                return title;
            }

            string clearName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title),
                   clearSite = siteName;

            clearName = clearName.Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

            clearName = Regex.Replace(clearName, @"[^a-zA-Z0-9 ]", " ");
            clearSite = Regex.Replace(clearSite, @"\W", string.Empty);

            var matched = false;
            while (!string.IsNullOrEmpty(clearSite) && clearName.Contains(" ", StringComparison.OrdinalIgnoreCase))
            {
                clearName = clearName.Replace(" ", string.Empty, 1, StringComparison.OrdinalIgnoreCase);
                if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }

            if ((matched || !clearName.Contains(" ")) && !string.IsNullOrEmpty(clearSite))
            {
                clearName = clearName.Replace(clearSite, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            clearName = string.Join(" ", clearName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            return clearName;
        }

        public static (string, DateTime?) GetDateFromTitle(string title)
        {
            string searchDate,
                   searchTitle = title;
            var regExRules = new Dictionary<string, string>
            {
                { @"\b[0-9]{4} [0-9]{2} [0-9]{2}\b", "yyyy MM dd" },
                { @"\b[0-9]{2} [0-9]{2} [0-9]{2}\b", "yy MM dd" },
            };
            (string, DateTime?) searchData = (searchTitle, null);

            foreach (var regExRule in regExRules)
            {
                var regEx = Regex.Match(searchTitle, regExRule.Key);
                if (regEx.Groups.Count > 0)
                {
                    if (DateTime.TryParseExact(regEx.Groups[0].Value, regExRule.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var searchDateObj))
                    {
                        searchDate = searchDateObj.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        searchTitle = Regex.Replace(searchTitle, regExRule.Key, string.Empty).Trim();

                        searchData = (searchTitle, searchDateObj);
                        break;
                    }
                }
            }

            return searchData;
        }

        public static string ReplaceAbbrieviation(string title)
        {
            var newTitle = title + " ";

            foreach (var abbrieviation in Database.SiteList.Abbrieviations)
            {
                var regex = new Regex(abbrieviation.Key + " ", RegexOptions.IgnoreCase);
                if (regex.IsMatch(newTitle))
                {
                    newTitle = regex.Replace(newTitle, abbrieviation.Value + " ", 1);
                    break;
                }
            }

            newTitle = newTitle.Trim();

            return newTitle;
        }

        public static IProviderBase GetProviderBySiteID(int siteID)
        {
            if (Database.SiteList.SiteIDList != null && Database.SiteList.SiteIDList.ContainsKey(siteID))
            {
                return GetBaseSiteByName(Database.SiteList.SiteIDList[siteID]);
            }

            return null;
        }

        public static IProviderBase GetBaseSiteByName(string name)
        {
            name = $"{typeof(Plugin).Namespace}.Sites.{name}";
            var provider = Type.GetType(name, false, true);

            if (provider != null)
            {
                return (IProviderBase)Activator.CreateInstance(provider);
            }

            return null;
        }

        public static async Task<List<RemoteSearchResult>> GetSearchResultsFromUpdate(IProviderBase provider, int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            var curID = new List<string>()
            {
                siteNum[0].ToString(CultureInfo.InvariantCulture),
                siteNum[1].ToString(CultureInfo.InvariantCulture),
            };

            curID.AddRange(sceneID);

            var sceneData = await provider.Update(siteNum, sceneID, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(sceneData.Item.Name))
            {
                sceneData.Item.ProviderIds.Add(Plugin.Instance.Name, string.Join("#", curID));
                var posters = (await provider.GetImages(siteNum, sceneID, sceneData.Item, cancellationToken).ConfigureAwait(false)).Where(item => item.Type == ImageType.Primary);

                var res = new RemoteSearchResult
                {
                    ProviderIds = sceneData.Item.ProviderIds,
                    Name = sceneData.Item.Name,
                    PremiereDate = sceneData.Item.PremiereDate,
                };

                if (!string.IsNullOrEmpty(sceneData.Item.OriginalTitle))
                {
                    res.Name = $"{sceneData.Item.OriginalTitle} {sceneData.Item.Name}";
                }

                if (posters.Any())
                {
                    res.ImageUrl = posters.First().Url;
                }

                result.Add(res);
            }

            return result;
        }

        public static string GetSiteNameFromTitle(string searchTitle)
        {
            searchTitle = GetClearTitle(searchTitle, string.Empty);

            var splitedTitle = searchTitle.Split();
            if (splitedTitle.Length == 2 && int.TryParse(splitedTitle[1], out _))
            {
                searchTitle = $"JAV {splitedTitle[0]}-{splitedTitle[1]}";
            }

            return searchTitle;
        }

        public static byte[] ConvertFromBase64String(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            try
            {
                var working = input.Replace('-', '+').Replace('_', '/');
                while (working.Length % 4 != 0)
                {
                    working += '=';
                }

                return Convert.FromBase64String(working);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
