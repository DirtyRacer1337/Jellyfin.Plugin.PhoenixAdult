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
            var cleanedGenres = new List<string>();

            if (genresLink == null)
            {
                return cleanedGenres.ToArray();
            }

            if (actors != null && actors.Any())
            {
                switch (actors.Count)
                {
                    case 1:
                    case 2:
                        break;
                    case 3:
                        cleanedGenres.Add("Threesome");
                        break;
                    case 4:
                        cleanedGenres.Add("Foursome");
                        break;
                    default:
                        cleanedGenres.Add("Orgy");
                        break;
                }
            }

            foreach (var genreLink in genresLink)
            {
                var genreName = WebUtility.HtmlDecode(genreLink).Trim();

                if (genreName.Contains(",", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var genre in genreName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var splitedGenre = genre.Trim();

                        cleanedGenres.Add(splitedGenre);
                    }
                }
                else
                {
                    cleanedGenres.Add(genreName);
                }
            }

            var newGenres = new List<string>();
            foreach (var genreLink in cleanedGenres)
            {
                var genreName = Replace(genreLink, sceneName);

                if (!string.IsNullOrEmpty(genreName))
                {
                    genreName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genreName);

                    if (!newGenres.Contains(genreName, StringComparer.OrdinalIgnoreCase))
                    {
                        newGenres.Add(genreName);
                    }
                }
            }

            return newGenres.OrderBy(o => o).ToArray();
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

            var newGenreName = Database.Genres.GenresReplace.FirstOrDefault(x => x.Key.Equals(genreName, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(genreName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newGenreName))
            {
                genreName = newGenreName;
            }
            else
            {
                foreach (var genreDic in Database.Genres.GenresPartialReplace)
                {
                    if (genreName.Equals(genreDic.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        genreName = genreDic.Key;
                    }
                    else
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
