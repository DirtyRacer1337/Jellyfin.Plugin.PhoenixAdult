using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Entities;
using PhoenixAdult.Configuration;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Helpers
{
    internal static class Genres
    {
        public static string[] Cleanup(string[] genresLink, string sceneName, List<PersonInfo> actors)
        {
            var cleanedGenres = new List<string>();

            if (genresLink == null || Plugin.Instance.Configuration.DisableGenres)
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
                var genreName = WebUtility.HtmlDecode(genreLink);
                genreName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genreName)
                    .Replace(" And ", " and ", StringComparison.OrdinalIgnoreCase)
                    .Replace(" To ", " to ", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (genreName.Contains(',', StringComparison.OrdinalIgnoreCase) || genreName.Contains('/', StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var genre in genreName.Split(new char[] { ',', '/' }, StringSplitOptions.RemoveEmptyEntries))
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
                var genreName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genreLink.ToLowerInvariant());
                genreName = Replace(genreName, sceneName);
                var genreNames = Split(genreName);

                if (genreNames.Any())
                {
                    foreach (var genre in genreNames)
                    {
                        genreName = Replace(genre, sceneName);

                        if (!string.IsNullOrEmpty(genreName) && !newGenres.Contains(genreName, StringComparer.OrdinalIgnoreCase))
                        {
                            newGenres.Add(genreName);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(genreName))
                {
                    if (!newGenres.Contains(genreName, StringComparer.OrdinalIgnoreCase))
                    {
                        newGenres.Add(genreName);
                    }
                }
            }

            switch (Plugin.Instance.Configuration.GenresSortingStyle)
            {
                case GenresSortingStyle.PositionsLast:
                    newGenres = newGenres.OrderBy(o => o.Contains("Position")).ThenBy(o => o).ToList();
                    break;

                default:
                    newGenres = newGenres.OrderBy(o => o).ToList();
                    break;
            }

            return newGenres.ToArray();
        }

        public static string[] Cleanup(string[] genresLink, string sceneName) => Cleanup(genresLink, sceneName, null);

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

            var newGenreName = Database.Genres.GenresReplace.FirstOrDefault(o => o.Key.Equals(genreName, StringComparison.OrdinalIgnoreCase) || o.Value.Contains(genreName, StringComparer.OrdinalIgnoreCase)).Key;
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
                if (genreName.Contains(':', StringComparison.OrdinalIgnoreCase))
                {
                    if (sceneName.Contains(genreName.Split(':').First(), StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }

                if (genreName.Contains('-', StringComparison.OrdinalIgnoreCase))
                {
                    if (sceneName.Contains(genreName.Split('-').First(), StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }
            }

            return genreName;
        }

        private static List<string> Split(string genreName)
        {
            var result = new List<string>();

            foreach (var genre in Database.Genres.GenresSplit)
            {
                if (genre.Key.Equals(genreName, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddRange(genre.Value);
                }
            }

            return result;
        }
    }
}
