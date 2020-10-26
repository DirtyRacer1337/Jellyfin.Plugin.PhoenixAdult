using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Entities;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Helpers
{
    internal static class Genres
    {
        public static string[] Cleanup(string[] genresLink, string sceneName, List<PersonInfo> actors)
        {
            var newGenres = new List<string>();

            if (genresLink == null)
            {
                return newGenres.ToArray();
            }

            if (actors != null && actors.Any())
            {
                switch (actors.Count)
                {
                    case 1:
                    case 2:
                        break;
                    case 3:
                        newGenres.Add("Threesome");
                        break;
                    case 4:
                        newGenres.Add("Foursome");
                        break;
                    default:
                        newGenres.Add("Orgy");
                        break;
                }
            }

            foreach (var genreLink in genresLink)
            {
                var genreName = WebUtility.HtmlDecode(genreLink).Trim();
                genreName = Replace(genreName, sceneName);

                if (!string.IsNullOrEmpty(genreName))
                {
                    genreName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genreName);

                    if (!newGenres.Contains(genreName))
                    {
                        newGenres.Add(genreName);
                    }
                }
            }

            return newGenres.OrderBy(item => item).ToArray();
        }

        public static string[] Cleanup(string[] genresLink, string sceneName)
            => Cleanup(genresLink, sceneName, null);

        private static string Replace(string genreName, string sceneName)
        {
            if (Database.Genres.GenresSkip.Contains(genreName, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            foreach (var genre in Database.Genres.GenresPartialSkip)
            {
                if (genreName.Contains(genre, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }

            var newGenreName = Database.Genres.GenresReplace.FirstOrDefault(x => x.Value.Contains(genreName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newGenreName))
            {
                genreName = newGenreName;
            }
            else
            {
                foreach (var genreDic in Database.Genres.GenresPartialReplace)
                {
                    foreach (var genre in genreDic.Value)
                    {
                        if (genreName.Contains(genre, StringComparison.OrdinalIgnoreCase))
                        {
                            genreName = genreDic.Key;
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(sceneName))
            {
                if (genreName.Contains(":", StringComparison.OrdinalIgnoreCase))
                {
                    if (sceneName.Contains(genreName.Split(':').First(), StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                if (genreName.Contains("-", StringComparison.OrdinalIgnoreCase))
                {
                    if (sceneName.Contains(genreName.Split('-').First(), StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                /*if (sceneName.Contains(genreName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(newGenreName))
                    return null;
                */
            }

            if (genreName.Length > 25 || genreName.Split().Length > 3)
            {
                return null;
            }

            return genreName;
        }
    }
}
