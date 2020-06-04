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
            if (actorName.Equals("Abby Lee", StringComparison.OrdinalIgnoreCase))
                return "Abby Lee Brazil";
            if (actorName.Equals("Abby Rains", StringComparison.OrdinalIgnoreCase))
                return "Abbey Rain";
            if (actorName.Equals("Ms Addie Juniper", StringComparison.OrdinalIgnoreCase))
                return "Addie Juniper";
            if (actorName.Equals("Adrianna Chechik", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Adriana Chechick", StringComparison.OrdinalIgnoreCase))
                return "Adriana Chechik";
            if (actorName.Equals("Alex D", StringComparison.OrdinalIgnoreCase))
                return "Alex D.";
            if (actorName.Equals("Alura Tnt Jenson", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Alura 'Tnt' Jenson", StringComparison.OrdinalIgnoreCase))
                return "Alura Jenson";
            if (actorName.Equals("Amia Moretti", StringComparison.OrdinalIgnoreCase))
                return "Amia Miley";
            if (actorName.Equals("Amy Reid", StringComparison.OrdinalIgnoreCase))
                return "Amy Ried";
            if (actorName.Equals("Ana Fox", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Ana Foxx", StringComparison.OrdinalIgnoreCase))
                return "Ana Foxxx";
            if (actorName.Equals("Andreina De Lux", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Andreina De Luxe", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Andreina Dlux", StringComparison.OrdinalIgnoreCase))
                return "Andreina Deluxe";
            if (actorName.Equals("Angela Piaf", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Angel Piaf", StringComparison.OrdinalIgnoreCase))
                return "Angel Piaff";
            if (actorName.Equals("Ani Black Fox", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Ani Black", StringComparison.OrdinalIgnoreCase))
                return "Ani Blackfox";
            if (actorName.Equals("Anikka Albrite", StringComparison.OrdinalIgnoreCase))
                return "Annika Albrite";
            if (actorName.Equals("Anita Bellini", StringComparison.OrdinalIgnoreCase))
                return "Anita Bellini Berlusconi";
            if (actorName.Equals("Anjelica", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Ebbi", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Abby H", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Katherine A", StringComparison.OrdinalIgnoreCase))
                return "Krystal Boyd";
            if (actorName.Equals("Anna Morna", StringComparison.OrdinalIgnoreCase))
                return "Anastasia Morna";
            if (actorName.Equals("April ONeil", StringComparison.OrdinalIgnoreCase) || actorName.Equals("April Oneil", StringComparison.OrdinalIgnoreCase) || actorName.Equals("April O'neil", StringComparison.OrdinalIgnoreCase))
                return "April O'Neil";
            if (actorName.Equals("Ashley Graham", StringComparison.OrdinalIgnoreCase))
                return "Ashlee Graham";
            if (actorName.Equals("Bella Danger", StringComparison.OrdinalIgnoreCase))
                return "Abella Danger";
            if (actorName.Equals("Bibi Jones", StringComparison.OrdinalIgnoreCase))
                return "Britney Beth";
            if (actorName.Equals("Bridgette B.", StringComparison.OrdinalIgnoreCase))
                return "Bridgette B";
            if (actorName.Equals("Capri Cavalli", StringComparison.OrdinalIgnoreCase))
                return "Capri Cavanni";
            if (actorName.Equals("Ce Ce Capella", StringComparison.OrdinalIgnoreCase))
                return "CeCe Capella";
            if (actorName.Equals("Charli Red", StringComparison.OrdinalIgnoreCase))
                return "Charlie Red";
            if (actorName.Equals("Charlotte Lee", StringComparison.OrdinalIgnoreCase))
                return "Jaye Summers";
            if (actorName.Equals("Criss Strokes", StringComparison.OrdinalIgnoreCase))
                return "Chris Strokes";
            if (actorName.Equals("Christy Charming", StringComparison.OrdinalIgnoreCase))
                return "Paula Shy";
            if (actorName.Equals("CléA Gaultier", StringComparison.OrdinalIgnoreCase))
                return "Clea Gaultier";
            if (actorName.Equals("Crissy Kay", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Emma Hicks", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Emma Hixx", StringComparison.OrdinalIgnoreCase))
                return "Emma Hix";
            if (actorName.Equals("Crystal Rae", StringComparison.OrdinalIgnoreCase))
                return "Cyrstal Rae";
            if (actorName.Equals("Doris Ivy", StringComparison.OrdinalIgnoreCase))
                return "Gina Gerson";
            if (actorName.Equals("Eden Sin", StringComparison.OrdinalIgnoreCase))
                return "Eden Sinclair";
            if (actorName.Equals("Elsa Dream", StringComparison.OrdinalIgnoreCase))
                return "Elsa Jean";
            if (actorName.Equals("Eve Lawrence", StringComparison.OrdinalIgnoreCase))
                return "Eve Laurence";
            if (actorName.Equals("Francesca Di Caprio", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Francesca Dicaprio", StringComparison.OrdinalIgnoreCase))
                return "Francesca DiCaprio";
            if (actorName.Equals("Guiliana Alexis", StringComparison.OrdinalIgnoreCase))
                return "Gulliana Alexis";
            if (actorName.Equals("Grace Hartley", StringComparison.OrdinalIgnoreCase))
                return "Pinky June";
            if (actorName.Equals("Hailey Reed", StringComparison.OrdinalIgnoreCase))
                return "Haley Reed";
            if (actorName.Equals("Josephina Jackson", StringComparison.OrdinalIgnoreCase))
                return "Josephine Jackson";
            if (actorName.Equals("Jane Doux", StringComparison.OrdinalIgnoreCase))
                return "Pristine Edge";
            if (actorName.Equals("Jade Indica", StringComparison.OrdinalIgnoreCase))
                return "Miss Jade Indica";
            if (actorName.Equals("Jassie Gold", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Jaggie Gold", StringComparison.OrdinalIgnoreCase))
                return "Jessi Gold";
            if (actorName.Equals("Jenna J Ross", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Jenna J. Ross", StringComparison.OrdinalIgnoreCase))
                return "Jenna Ross";
            if (actorName.Equals("Jenny Ferri", StringComparison.OrdinalIgnoreCase))
                return "Jenny Fer";
            if (actorName.Equals("Jessica Blue", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Jessica Cute", StringComparison.OrdinalIgnoreCase))
                return "Jessica Foxx";
            if (actorName.Equals("Jo Jo Kiss", StringComparison.OrdinalIgnoreCase))
                return "Jojo Kiss";
            if (actorName.Equals("Josephine", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Conny", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Conny Carter", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Connie", StringComparison.OrdinalIgnoreCase))
                return "Connie Carter";
            if (actorName.Equals("Kagney Lynn Karter", StringComparison.OrdinalIgnoreCase))
                return "Kagney Linn Karter";
            if (actorName.Equals("Kari Sweets", StringComparison.OrdinalIgnoreCase))
                return "Kari Sweet";
            if (actorName.Equals("Katarina", StringComparison.OrdinalIgnoreCase))
                return "Katerina Hartlova";
            if (actorName.Equals("Kendra May Lust", StringComparison.OrdinalIgnoreCase))
                return "Kendra Lust";
            if (actorName.Equals("Khloe Capri", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Chloe Capri", StringComparison.OrdinalIgnoreCase))
                return "Khloe Kapri";
            if (actorName.Equals("Lara Craft", StringComparison.OrdinalIgnoreCase))
                return "Lora Craft";
            if (actorName.Equals("Lilly LaBeau", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Lilly Labuea", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Lily La Beau", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Lily Lebeau", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Lily Luvs", StringComparison.OrdinalIgnoreCase))
                return "Lily Labeau";
            if (actorName.Equals("Lilly Lit", StringComparison.OrdinalIgnoreCase))
                return "Lilly Ford";
            if (actorName.Equals("Maddy OReilly", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Maddy O'reilly", StringComparison.OrdinalIgnoreCase))
                return "Maddy O'Reilly";
            if (actorName.Equals("Maria Rya", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Melena Maria", StringComparison.OrdinalIgnoreCase))
                return "Melena Maria Rya";
            if (actorName.Equals("Moe The Monster Johnson", StringComparison.OrdinalIgnoreCase))
                return "Moe Johnson";
            if (actorName.Equals("Nadya Nabakova", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Nadya Nabokova", StringComparison.OrdinalIgnoreCase))
                return "Bunny Colby";
            if (actorName.Equals("Nancy A.", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Nancy A", StringComparison.OrdinalIgnoreCase))
                return "Nancy Ace";
            if (actorName.Equals("Nathaly", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Nathalie Cherie", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Natalie Cherie", StringComparison.OrdinalIgnoreCase))
                return "Nathaly Cherie";
            if (actorName.Equals("Nika Noir", StringComparison.OrdinalIgnoreCase))
                return "Nika Noire";
            if (actorName.Equals("Noe Milk", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Noemiek", StringComparison.OrdinalIgnoreCase))
                return "Noemilk";
            if (actorName.Equals("Remy La Croix", StringComparison.OrdinalIgnoreCase))
                return "Remy Lacroix";
            if (actorName.Equals("Riley Jenson", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Riley Anne", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Rilee Jensen", StringComparison.OrdinalIgnoreCase))
                return "Riley Jensen";
            if (actorName.Equals("Sara Luv", StringComparison.OrdinalIgnoreCase))
                return "Sara Luvv";
            if (actorName.Equals("Dylann Vox", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Dylan Vox", StringComparison.OrdinalIgnoreCase))
                return "Skylar Vox";
            if (actorName.Equals("Sedona", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Stefanie Renee", StringComparison.OrdinalIgnoreCase))
                return "Stephanie Renee";
            if (actorName.Equals("Stella Bankxxx", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Stella Ferrari", StringComparison.OrdinalIgnoreCase))
                return "Stella Banxxx";
            if (actorName.Equals("Steven St.Croix", StringComparison.OrdinalIgnoreCase))
                return "Steven St. Croix";
            if (actorName.Equals("Sybil Kailena", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Sybil", StringComparison.OrdinalIgnoreCase))
                return "Sybil A";
            if (actorName.Equals("Tiny Teen", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Tieny Mieny", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Lady Jay", StringComparison.OrdinalIgnoreCase) || actorName.Equals("Tiny Teen / Eva Elfie", StringComparison.OrdinalIgnoreCase))
                return "Eva Elfie";
            if (actorName.Equals("Veronica Vega", StringComparison.OrdinalIgnoreCase))
                return "Veronica Valentine";

            foreach (var studio in studios)
                switch (studio)
                {
                    case "21Sextury":
                    case "Footsie Babes":
                        if (actorName.Equals("Abbie", StringComparison.OrdinalIgnoreCase))
                            return actorName = "Krystal Boyd";
                        if (actorName.Equals("Ariel Temple", StringComparison.OrdinalIgnoreCase))
                            return actorName = "Katarina Muti";
                        if (actorName.Equals("Henna Ssy", StringComparison.OrdinalIgnoreCase))
                            return actorName = "Henessy";
                        break;
                }

            return actorName;
        }
    }
}
