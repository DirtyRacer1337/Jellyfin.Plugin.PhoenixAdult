using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Helpers
{
    public static class PhoenixAdultHelper
    {
        public static CultureInfo Lang = new CultureInfo("en-US", false);

        public static string GetSearchSiteName(int[] siteNum)
        {
            if (siteNum == null)
                return null;

            return PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]][0];
        }

        public static string GetSearchBaseURL(int[] siteNum)
        {
            if (siteNum == null)
                return null;

            if (!string.IsNullOrEmpty(PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]].ElementAtOrDefault(1)))
                return PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]][1];
            else
                return PhoenixAdultList.SiteList[siteNum[0]].First().Value[1];
        }

        public static string GetSearchSearchURL(int[] siteNum)
        {
            if (siteNum == null)
                return null;

            if (!string.IsNullOrEmpty(PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]].ElementAtOrDefault(2)))
                return PhoenixAdultList.SiteList[siteNum[0]][siteNum[1]][2];
            else
                return PhoenixAdultList.SiteList[siteNum[0]].First().Value[2];
        }

        public static int LevenshteinDistance(string source1, string source2)
        {
            if (source1 == null || source2 == null)
                return -1;

            var source1Length = source1.Length;
            var source2Length = source2.Length;

            var matrix = new int[source1Length + 1, source2Length + 1];

            if (source1Length == 0)
                return source2Length;

            if (source2Length == 0)
                return source1Length;

            for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
            for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[source1Length, source2Length];
        }

        public static string Encode(string text)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).Replace('=', '#');

        public static string Decode(string base64Text)
        {
            if (base64Text != null)
                return Encoding.UTF8.GetString(Convert.FromBase64String(base64Text.Replace('#', '=')));
            return null;
        }

        public static Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
            => PhoenixAdultProvider.Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
    }
}
