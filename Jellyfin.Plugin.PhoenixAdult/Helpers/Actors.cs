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
using Jellyfin.Data.Enums;
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
                    Name = people.Name,
                    Type = people.Type,
                    ImageUrl = people.ImageUrl,
                };

                newPeople.Name = WebUtility.HtmlDecode(newPeople.Name);
                newPeople.Name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(newPeople.Name);
                newPeople.Name = newPeople.Name.Split('(').First();
                newPeople.Name = newPeople.Name.Replace("™", string.Empty, StringComparison.OrdinalIgnoreCase);
                newPeople.Name = newPeople.Name.Trim();

#if __EMBY__
#else
                if (string.IsNullOrEmpty(newPeople.Type.ToString()))
                {
                    newPeople.Type = PersonKind.Actor;
                }
#endif
                if (!newPeoples.Any(o => o.Name == newPeople.Name))
                {
                    newPeoples.Add(newPeople);
                }
            }

            switch (Plugin.Instance.Configuration.PreferedActorNameSource)
            {
                case PreferedActorNameSource.LocalDatabase:
                    newPeoples = CleanupFromDatabase(newPeoples, scene.Item);
                    break;
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

        public static List<PersonInfo> CleanupFromDatabase(List<PersonInfo> peoples, BaseItem item)
        {
            var newPeoples = new List<PersonInfo>();

            foreach (var people in peoples)
            {
                var newPeople = new PersonInfo
                {
                    Name = people.Name,
                    Type = people.Type,
                    ImageUrl = people.ImageUrl,
                };

                var newName = ReplaceFromDatabase(newPeople.Name, item.Studios);
                if (newName == newPeople.Name)
                {
                    switch (Plugin.Instance.Configuration.JAVActorNamingStyle)
                    {
                        case JAVActorNamingStyle.JapaneseStyle:
                            string japaneseName = string.Join(" ", newName.Split().Reverse()),
                                newJapaneseName = ReplaceFromDatabase(japaneseName, item.Studios);

                            newJapaneseName = string.Join(" ", newJapaneseName.Split().Reverse());

                            if (newJapaneseName != japaneseName)
                            {
                                newName = newJapaneseName;
                            }

                            break;
                    }
                }

                newPeople.Name = newName;

                if (!newPeoples.Any(o => o.Name == newPeople.Name))
                {
                    newPeoples.Add(newPeople);
                }
            }

            return newPeoples;
        }

        private static string ReplaceFromDatabase(string actorName, string[] studios)
        {
            var siteIndex = -1;
            foreach (var studio in studios)
            {
                var studioName = studio.Split('!').First().Trim();

                foreach (var studioIndex in Database.Actors.ActorsStudioIndexes)
                {
                    if (studioIndex.Value.Contains(studioName))
                    {
                        siteIndex = studioIndex.Key;
                        break;
                    }
                }

                if (siteIndex > -1)
                {
                    break;
                }
            }

            var newActorName = string.Empty;
            if (siteIndex > -1)
            {
                newActorName = Database.Actors.ActorsReplaceStudios[siteIndex].FirstOrDefault(o => o.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
                if (!string.IsNullOrEmpty(newActorName))
                {
                    actorName = newActorName;
                }
            }

            newActorName = Database.Actors.ActorsReplace.FirstOrDefault(o => o.Key.Equals(actorName, StringComparison.OrdinalIgnoreCase) || o.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newActorName))
            {
                actorName = newActorName;
            }

            return actorName;
        }
    }
}
