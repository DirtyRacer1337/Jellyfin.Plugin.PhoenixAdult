using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Helpers
{
    internal static class Actors
    {
        public static List<PersonInfo> Cleanup(MetadataResult<Movie> scene)
        {
            var newPeoples = new List<PersonInfo>();

            if (scene == null)
            {
                return newPeoples;
            }

            foreach (var people in scene.People)
            {
                var newPeople = new PersonInfo
                {
                    Type = PersonType.Actor,
                    ImageUrl = people.ImageUrl,
                };
                newPeople.Name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(people.Name);
                newPeople.Name = newPeople.Name.Split('(').First().Trim();
                newPeople.Name = newPeople.Name.Replace("™", string.Empty, StringComparison.OrdinalIgnoreCase);
                newPeople.Name = Replace(newPeople.Name, scene.Item.Studios);

                if (!newPeoples.Any(person => person.Name == newPeople.Name))
                {
                    newPeoples.Add(newPeople);
                }
            }

            return newPeoples;
        }

        public static List<PersonInfo> Cleanup(List<PersonInfo> peoples, BaseItem item)
        {
            return Cleanup(new MetadataResult<Movie>
            {
                People = peoples,
                Item = new Movie
                {
                    Studios = item.Studios,
                },
            });
        }

        private static string Replace(string actorName, string[] studios)
        {
            var newActorName = Database.Actors.ActorsReplace.FirstOrDefault(x => x.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newActorName))
            {
                return newActorName;
            }

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
                {
                    return newActorName;
                }
            }

            return actorName;
        }
    }
}
