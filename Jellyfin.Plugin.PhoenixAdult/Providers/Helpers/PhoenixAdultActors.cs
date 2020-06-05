using System;
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
                people.Name = people.Name.Replace("™", string.Empty, StringComparison.OrdinalIgnoreCase);
                people.Name = Replace(people.Name, item.Item.Studios);

                if (!newPeoples.Any(person => person.Name == people.Name))
                    newPeoples.Add(people);
            }

            return newPeoples;
        }

        private static string Replace(string actorName, string[] studios)
        {
            var newActorName = _replaceList.FirstOrDefault(x => x.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newActorName))
                return newActorName;

            int siteIndex = -1;
            foreach (var studio in studios)
                switch (studio)
                {
                    case "21Sextury":
                    case "Footsie Babes":
                        siteIndex = 0;
                        break;
                }

            if (siteIndex > -1)
            {
                newActorName = _replaceListStudio[siteIndex].FirstOrDefault(item => item.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
                if (!string.IsNullOrEmpty(newActorName))
                    return newActorName;
            }

            return actorName;
        }

        private static readonly Dictionary<string, string[]> _replaceList = new Dictionary<string, string[]> {
            { "Abby Lee Brazil", new string[] { "Abby Lee" } },
            { "Abbey Rain", new string[] { "Abby Rains" } },
            { "Addie Juniper", new string[] { "Ms Addie Juniper" } },
            { "Adriana Chechik", new string[] { "Adrianna Chechik", "Adriana Chechick" } },
            { "Alex D.", new string[] { "Alex D" } },
            { "Alura Jenson", new string[] { "Alura Tnt Jenson", "Alura 'Tnt' Jenson" } },
            { "Amia Miley", new string[] { "Amia Moretti" } },
            { "Amy Ried", new string[] { "Amy Reid" } },
            { "Ana Foxxx", new string[] { "Ana Fox", "Ana Foxx" } },
            { "Andreina Deluxe", new string[] { "Andreina De Lux", "Andreina De Luxe", "Andreina Dlux" } },
            { "Angel Piaff", new string[] { "Angela Piaf", "Angel Piaf" } },
            { "Ani Blackfox", new string[] { "Ani Black Fox", "Ani Black" } },
            { "Annika Albrite", new string[] { "Anikka Albrite" } },
            { "Anita Bellini Berlusconi", new string[] { "Anita Bellini" } },
            { "Krystal Boyd", new string[] { "Anjelica", "Ebbi", "Abby H", "Katherine A" } },
            { "Anastasia Morna", new string[] { "Anna Morna" } },
            { "April O'Neil", new string[] { "April ONeil", "April O'neil" } },
            { "Ashlee Graham", new string[] { "Ashley Graham" } },
            { "Abella Danger", new string[] { "Bella Danger" } },
            { "Britney Beth", new string[] { "Bibi Jones" } },
            { "Bridgette B", new string[] { "Bridgette B." } },
            { "Capri Cavanni", new string[] { "Capri Cavalli" } },
            { "CeCe Capella", new string[] { "Ce Ce Capella" } },
            { "Charlie Red", new string[] { "Charli Red" } },
            { "Jaye Summers", new string[] { "Charlotte Lee" } },
            { "Chris Strokes", new string[] { "Criss Strokes" } },
            { "Paula Shy", new string[] { "Christy Charming" } },
            { "Clea Gaultier", new string[] { "CléA Gaultier" } },
            { "Emma Hix", new string[] { "Crissy Kay", "Emma Hicks", "Emma Hixx" } },
            { "Cyrstal Rae", new string[] { "Crystal Rae" } },
            { "Gina Gerson", new string[] { "Doris Ivy" } },
            { "Eden Sinclair", new string[] { "Eden Sin" } },
            { "Elsa Jean", new string[] { "Elsa Dream" } },
            { "Eve Laurence", new string[] { "Eve Lawrence" } },
            { "Francesca DiCaprio", new string[] { "Francesca Di Caprio" } },
            { "Gulliana Alexis", new string[] { "Guiliana Alexis" } },
            { "Pinky June", new string[] { "Grace Hartley" } },
            { "Haley Reed", new string[] { "Hailey Reed" } },
            { "Josephine Jackson", new string[] { "Josephina Jackson" } },
            { "Pristine Edge", new string[] { "Jane Doux" } },
            { "Miss Jade Indica", new string[] { "Jade Indica" } },
            { "Jessi Gold", new string[] { "Jassie Gold", "Jaggie Gold" } },
            { "Jenna Ross", new string[] { "Jenna J Ross", "Jenna J. Ross" } },
            { "Jenny Fer", new string[] { "Jenny Ferri" } },
            { "Jessica Foxx", new string[] { "Jessica Blue", "Jessica Cute" } },
            { "Jojo Kiss", new string[] { "Jo Jo Kiss" } },
            { "Connie Carter", new string[] { "Josephine", "Conny", "Conny Carter", "Connie" } },
            { "Kagney Linn Karter", new string[] { "Kagney Lynn Karter" } },
            { "Kari Sweet", new string[] { "Kari Sweets" } },
            { "Katerina Hartlova", new string[] { "Katarina" } },
            { "Kendra Lust", new string[] { "Kendra May Lust" } },
            { "Khloe Kapri", new string[] { "Khloe Capri", "Chloe Capri" } },
            { "Lora Craft", new string[] { "Lara Craft" } },
            { "Lily Labeau", new string[] { "Lilly LaBeau", "Lilly Labuea", "Lily La Beau", "Lily Luvs" } },
            { "Lilly Ford", new string[] { "Lilly Lit" } },
            { "Maddy O'Reilly", new string[] { "Maddy OReilly", "Maddy O'reilly" } },
            { "Melena Maria Rya", new string[] { "Maria Rya", "Melena Maria" } },
            { "Moe Johnson", new string[] { "Moe The Monster Johnson" } },
            { "Bunny Colby", new string[] { "Nadya Nabakova", "Nadya Nabokova" } },
            { "Nancy Ace", new string[] { "Nancy A.", "Nancy A" } },
            { "Nathaly Cherie", new string[] { "Nathaly", "Nathalie Cherie", "Natalie Cherie" } },
            { "Nika Noire", new string[] { "Nika Noir" } },
            { "Noemilk", new string[] { "Noe Milk", "Noemiek" } },
            { "Remy Lacroix", new string[] { "Remy La Croix" } },
            { "Riley Jensen", new string[] { "Riley Jenson", "Riley Anne", "Rilee Jensen" } },
            { "Sara Luvv", new string[] { "Sara Luv" } },
            { "Skylar Vox", new string[] { "Dylann Vox", "Dylan Vox" } },
            { "Stephanie Renee", new string[] { "Sedona", "Stefanie Renee" } },
            { "Stella Banxxx", new string[] { "Stella Bankxxx", "Stella Ferrari" } },
            { "Steven St. Croix", new string[] { "Steven St.Croix" } },
            { "Sybil A", new string[] { "Sybil Kailena", "Sybil" } },
            { "Eva Elfie", new string[] { "Tiny Teen", "Tieny Mieny", "Lady Jay", "Tiny Teen / Eva Elfie" } },
            { "Veronica Valentine", new string[] { "Veronica Vega" } },
        };

        private static readonly Dictionary<int, Dictionary<string, string[]>> _replaceListStudio = new Dictionary<int, Dictionary<string, string[]>> {{
                0, new Dictionary<string, string[]> {
                    { "Krystal Boyd", new string[] { "Abbie" } },
                    { "Katarina Muti", new string[] { "Ariel Temple" } },
                    { "Henessy", new string[] { "Henna Ssy" } },
                }
            },
        };
    }
}
