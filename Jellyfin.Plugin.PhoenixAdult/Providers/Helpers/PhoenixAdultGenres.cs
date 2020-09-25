using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace PhoenixAdult.Helpers
{
    internal static class PhoenixAdultGenres
    {
        public static string[] Cleanup(string[] genresLink, string sceneName)
        {
            var newGenres = new List<string>();

            if (genresLink == null)
                return newGenres.ToArray();

            foreach (var genreLink in genresLink)
            {
                var genreName = WebUtility.HtmlDecode(genreLink).Trim();
                genreName = Replace(genreName, sceneName);

                if (!string.IsNullOrEmpty(genreName))
                {
                    genreName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genreName);

                    if (!newGenres.Contains(genreName))
                        newGenres.Add(genreName);
                }
            }

            return newGenres.OrderBy(item => item).ToArray();
        }

        private static string Replace(string genreName, string sceneName)
        {
            if (Database.GenresSkipList.Contains(genreName, StringComparer.OrdinalIgnoreCase))
                return null;

            foreach (var genre in Database.GenresSkipListPartial)
                if (genreName.Contains(genre, StringComparison.OrdinalIgnoreCase))
                    return null;

            if (genreName.Contains("doggystyle", StringComparison.OrdinalIgnoreCase) || genreName.Contains("doggy style", StringComparison.OrdinalIgnoreCase))
                return "Doggystyle (Position)";

            var newGenreName = Database.GenresReplaceList.FirstOrDefault(x => x.Value.Contains(genreName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newGenreName))
                genreName = newGenreName;

            if (!string.IsNullOrEmpty(sceneName))
            {
                if (genreName.Contains(":", StringComparison.OrdinalIgnoreCase))
                    if (sceneName.Contains(genreName.Split(':').First(), StringComparison.OrdinalIgnoreCase))
                        return null;

                if (genreName.Contains("-", StringComparison.OrdinalIgnoreCase))
                    if (sceneName.Contains(genreName.Split('-').First(), StringComparison.OrdinalIgnoreCase))
                        return null;

                /*if (sceneName.Contains(genreName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(newGenreName))
                    return null;
                */
            }

            if (genreName.Length > 25 || genreName.Split().Length > 3)
                return null;

            return genreName;
        }
    }
}
