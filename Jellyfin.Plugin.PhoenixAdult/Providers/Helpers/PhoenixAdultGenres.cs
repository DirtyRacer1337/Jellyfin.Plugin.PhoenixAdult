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

                if (!newGenres.Contains(genreName))
                    newGenres.Add(genreName);
            }

            return newGenres.ToArray();
        }
    }
}
