using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using PhoenixAdult.Configuration;
using PhoenixAdult.Helpers.Utils;

#if __EMBY__
#else
using MediaBrowser.Model.Entities;
#endif

namespace PhoenixAdult.Helpers
{
    internal static class Actors
    {
        public static List<PersonInfo> Cleanup(MetadataResult<Movie> scene)
        {
            var newPeoples = new List<PersonInfo>();

            if (scene == null || Plugin.Instance.Configuration.DisableActors)
            {
                return newPeoples;
            }

            foreach (var people in scene.People)
            {
                var newPeople = new PersonInfo
                {
                    Type = people.Type,
                    ImageUrl = people.ImageUrl,
                };

#if __EMBY__
#else
                if (string.IsNullOrEmpty(newPeople.Type))
                {
                    newPeople.Type = PersonType.Actor;
                }
#endif

                newPeople.Name = WebUtility.HtmlDecode(newPeople.Name);
                newPeople.Name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(people.Name);
                newPeople.Name = newPeople.Name.Split('(').First();
                newPeople.Name = newPeople.Name.Replace("™", string.Empty, StringComparison.OrdinalIgnoreCase);
                newPeople.Name = newPeople.Name.Trim();

                var newName = Replace(newPeople.Name, scene.Item.Studios);

                if (newName == newPeople.Name)
                {
                    if (Plugin.Instance.Configuration.JAVActorNamingStyle == JAVActorNamingStyle.JapaneseStyle)
                    {
                        string japaneseName = string.Join(" ", newPeople.Name.Split().Reverse()),
                            newJapaneseName = Replace(japaneseName, scene.Item.Studios);

                        newJapaneseName = string.Join(" ", newJapaneseName.Split().Reverse());

                        if (newJapaneseName != japaneseName)
                        {
                            newName = newJapaneseName;
                        }
                    }
                }

                newPeople.Name = newName;

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
