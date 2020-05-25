using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Helpers
{
    public static class PhoenixAdultPeoples
    {
        public static List<PersonInfo> Cleanup(MetadataResult<Movie> item)
        {
            var newPeoples = new List<PersonInfo>();

            if (item == null)
                return newPeoples;

            foreach (var people in item.People)
            {
                people.Name = PhoenixAdultHelper.Lang.TextInfo.ToTitleCase(people.Name);
                people.Name = people.Name.Split("(").First().Trim();

                if (!newPeoples.Any(person => person.Name == people.Name))
                    newPeoples.Add(people);
            }

            return newPeoples;
        }
    }
}
