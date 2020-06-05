using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Helpers
{
    public static class PhoenixAdultGenres
    {
        public static string[] Cleanup(string[] genresLink)
        {
            var newGenres = new List<string>();

            if (genresLink == null)
                return newGenres.ToArray();

            foreach (var genreLink in genresLink)
            {
                var genreName = PhoenixAdultHelper.Lang.TextInfo.ToTitleCase(genreLink);

                genreName = genreName.Split("(").First().Trim();
                genreName = Replace(genreName);

                if (!string.IsNullOrEmpty(genreName) && !newGenres.Contains(genreName))
                    newGenres.Add(genreName);
            }

            return newGenres.ToArray();
        }

        private static string Replace(string genreName)
        {
            if (_skipList.Contains(genreName, StringComparer.OrdinalIgnoreCase))
                return null;

            return genreName;
        }

        private static readonly List<string> _skipList = new List<string> {
            "18+ teens", "18+teens", "4k", "60p", "acworthy", "april showers", "april",
            "babe", "babes", "beaverday", "bonus", "daylight savings", "desert apocalypse",
            "destruction", "episode", "exclusive", "extra update", "faces of pain", "feel me",
            "get sprung", "gonzo", "grey", "hd videos", "heavymetal", "irreconcilable slut",
            "keiran", "little runaway", "mr. cummings", "photos", "public disgrace's best",
            "ryan mclane", "show less", "show more", "site member", "smart", "spring cleaning",
            "st patrick's day", "the pope", "tj cummings", "tony", "twistys hard", "tyler steele",
            "van styles", "workitout", "wow girls special", "yes, mistress", "5k", "60fps", "hd",
            "1080p", "aprilfools", "chibbles", "folsom",
        };
    }
}
