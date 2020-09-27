using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace PhoenixAdult.Helpers
{
    internal static class PhoenixAdultActors
    {
        public static List<PersonInfo> Cleanup(MetadataResult<Movie> scene)
        {
            var newPeoples = new List<PersonInfo>();

            if (scene == null)
                return newPeoples;

            foreach (var people in scene.People)
            {
                people.Type = PersonType.Actor;
                people.Name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(people.Name);
                people.Name = people.Name.Split('(').First().Trim();
                people.Name = people.Name.Replace("™", string.Empty, StringComparison.OrdinalIgnoreCase);
                people.Name = Replace(people.Name, scene.Item.Studios);

                if (!newPeoples.Any(person => person.Name == people.Name))
                    newPeoples.Add(people);
            }

            return newPeoples;
        }

        private static string Replace(string actorName, string[] studios)
        {
            var newActorName = Database.Actors.ActorsReplace.FirstOrDefault(x => x.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newActorName))
                return newActorName;

            int siteIndex = -1;
            foreach (var studio in studios)
            {
                var studioName = studio.Split('!').First().Trim();

                foreach (var studioIndex in Database.Actors.ActorsStudioIndexes)
                {
                    if (studioIndex.Value.Contains(studioName))
                    {
                        siteIndex = studioIndex.Key;
                    }
                }
            }

            if (siteIndex > -1)
            {
                newActorName = Database.Actors.ActorsReplaceStudios[siteIndex].FirstOrDefault(item => item.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
                if (!string.IsNullOrEmpty(newActorName))
                    return newActorName;
            }

            return actorName;
        }
    }
}
