using System.Collections.Generic;
using Jellyfin.Plugin.PhoenixAdult.Providers.Sites;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public static class PhoenixAdultList
    {
        public static Dictionary<int, Dictionary<int, string[]>> SiteList = new Dictionary<int, Dictionary<int, string[]>> {{
                    0, new Dictionary<int, string[]> {
                        {0, new string [] { "Brazzers", "http://www.brazzers.com", "https://www.brazzers.com/videos-search/" } },
                        {1, new string [] { "Asses In Public" } },
                        {2, new string [] { "Baby Got Boobs" } },
                        {3, new string [] { "Big Butts Like It Big" } },
                        {4, new string [] { "Big Tits at School" } },
                        {5, new string [] { "Big Tits at Work" } },
                        {6, new string [] { "Big Tits in Sports" } },
                        {7, new string [] { "Big Tits in Uniform" } },
                        {8, new string [] { "Big Wet Butts" } },
                        {9, new string [] { "Busty and Real" } },
                        {10, new string [] { "Busty Z" } },
                        {11, new string [] { "Butts and Blacks" } },
                        {12, new string [] { "CFNM Clothed Female Male Nude" } },
                        {13, new string [] { "Day With A Porn Star" } },
                        {14, new string [] { "Dirty Masseur" } },
                        {15, new string [] { "Doctor Adventures" } },
                        {16, new string [] { "Exxtra" } },
                        {17, new string [] { "Hot and Mean" } },
                        {18, new string [] { "Hot Chicks Big Asses" } },
                        {19, new string [] { "Milfs Like It Big" } },
                        {20, new string [] { "Mommy Got Boobs" } },
                        {21, new string [] { "Moms in Control" } },
                        {22, new string [] { "Pornstars Like It Big" } },
                        {23, new string [] { "Racks and Blacks" } },
                        {24, new string [] { "Real Wife Stories" } },
                        {25, new string [] { "Shes Gonna Squirt" } },
                        {26, new string [] { "Teens Like It Big" } },
                        {27, new string [] { "Teens Like It Black" } },
                        {28, new string [] { "ZZ Series" } },
                    }
                },{
                    1, new Dictionary<int, string[]> {
                        {0, new string [] { "Bang", "https://www.bang.com", "https://617fb597b659459bafe6472470d9073a.us-east-1.aws.found.io/videos/video/_search" } },
                    }
                }
            };

        public static IPhoenixAdultProviderBase GetProviderBySiteID(int siteID) => siteID switch
        {
            0 => new NetworkBrazzers(),
            1 => new NetworkBang(),
            _ => null,
        };
    }
}
