using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.PhoenixAdult.Providers.Helpers
{
    public static class PhoenixAdultGenres
    {
        public static string[] Cleanup(string[] genresLink, string sceneName)
        {
            var newGenres = new List<string>();

            if (genresLink == null)
                return newGenres.ToArray();

            foreach (var genreLink in genresLink)
            {
                var genreName = Replace(genreLink, sceneName);
                if (!string.IsNullOrEmpty(genreName))
                {
                    genreName = PhoenixAdultHelper.Lang.TextInfo.ToTitleCase(genreName);

                    if (!newGenres.Contains(genreName))
                        newGenres.Add(genreName);
                }
            }

            return newGenres.OrderBy(item => item).ToArray();
        }

        private static string Replace(string genreName, string sceneName)
        {
            if (_skipList.Contains(genreName, StringComparer.OrdinalIgnoreCase))
                return null;

            if (genreName.Contains("5k", StringComparison.OrdinalIgnoreCase)
                || genreName.Contains("60fps", StringComparison.OrdinalIgnoreCase)
                || genreName.Contains("hd", StringComparison.OrdinalIgnoreCase)
                || genreName.Contains("1080p", StringComparison.OrdinalIgnoreCase)
                || genreName.Contains("aprilfools", StringComparison.OrdinalIgnoreCase)
                || genreName.Contains("chibbles", StringComparison.OrdinalIgnoreCase)
                || genreName.Contains("folsom", StringComparison.OrdinalIgnoreCase)
            )
                return null;

            if (genreName.Contains("doggystyle", StringComparison.OrdinalIgnoreCase) || genreName.Contains("doggy style", StringComparison.OrdinalIgnoreCase))
                return "Doggystyle (Position)";

            var newGenreName = _replaceList.FirstOrDefault(x => x.Value.Contains(genreName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newGenreName))
                genreName = newGenreName;

            if (!string.IsNullOrEmpty(sceneName))
            {
                if (genreName.Contains(':', StringComparison.OrdinalIgnoreCase))
                    if (sceneName.Contains(genreName.Split(':').First(), StringComparison.OrdinalIgnoreCase))
                        return null;

                if (genreName.Contains('-', StringComparison.OrdinalIgnoreCase))
                    if (sceneName.Contains(genreName.Split('-').First(), StringComparison.OrdinalIgnoreCase))
                        return null;

                /*if (sceneName.Contains(genreName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(newGenreName))
                    return null;*/
            }

            if (genreName.Length > 25 || genreName.Split().Length > 3)
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
            "van styles", "workitout", "wow girls special", "yes, mistress",
        };

        private static readonly Dictionary<string, string[]> _replaceList = new Dictionary<string, string[]> {
            {"18-Year-Old", new string[] { "eighteen and...", "18", "18 year" } },
            {"All Natural", new string[] { "natural" } },
            {"Amateur", new string[] { "amatuer", "amateur pre auditions" } },
            {"Anal Fingering", new string[] { "ass fingering" } },
            {"Asian", new string[] { "asian amateur" } },
            {"Ass Eating", new string[] { "ass licking" } },
            {"Ass to Mouth", new string[] { "atm" } },
            {"Ass", new string[] { "butt", "booty" } },
            {"Athletic Body", new string[] { "athletic", "athlete" } },
            {"BBW", new string[] { "big beautiful women" } },
            {"Ball Gag", new string[] { "ballgagged" } },
            {"Ball Licking", new string[] { "ball licking", "ball sucking", "ball lick" } },
            {"Bareback", new string[] { "no condom" } },
            {"Barely Legal", new string[] { "barely-legal" } },
            {"Behind The Scenes", new string[] { "behind the scene" } },
            {"Big Butt", new string[] { "big ass", "big booty", "bib booty", "girl big ass", "big butts", "fat ass" } },
            {"Big Dick", new string[] { "big cock", "big cocks", "big dicks", "2 big cocks" } },
            {"Big Natural Tits", new string[] { "big naturals" } },
            {"Big Nipples", new string[] { "big areolas" } },
            {"Big Tits", new string[] { "big boobs", "bit tits", "girl big tits", "large tits" } },
            {"Big Toys", new string[] { "big toy" } },
            {"Bikini", new string[] { "bikin" } },
            {"Blonde", new string[] { "blond", "blonde hair", "blondes", "blond hair" } },
            {"Blow Bang", new string[] { "blowbang" } },
            {"Blow Job", new string[] { "blowjob", "blowjob (pov)", "blowjob (double)", "bj", "amateur blowjobs", "blowjobs", "blowjob - pov", "blow jobs", "blowjob - double" } },
            {"Boots", new string[] { "boot" } },
            {"Boy-Boy-Boy-Girl", new string[] { "mmmf" } },
            {"Boy-Boy-Girl", new string[] { "threesome bbg", "mmf", "bbg" } },
            {"Boy-Boy-Girl-Girl", new string[] { "mmff" } },
            {"Boy-Boy-Girl-Girl-Girl", new string[] { "fffmm" } },
            {"Boy-Girl", new string[] { "girl-boy", "girl/boy", "boy girl" } },
            {"Boy-Girl-Girl", new string[] { "bgg", "threesome bgg", "girl-girl-boy", "ffm", "2 girl bj" } },
            {"Breast Bondage", new string[] { "best bound breasts" } },
            {"Brunette", new string[] { "brown hair", "brunettes", "brunet" } },
            {"Bukkake", new string[] { "bukakke" } },
            {"Butt Plug", new string[] { "buttplug", "anal plug" } },
            {"CBT", new string[] { "death defying cbt" } },
            {"Camel Toe", new string[] { "camel toe pussy" } },
            {"Caucasian", new string[] { "white", "white girl", "while" } },
            {"Chastity Play", new string[] { "chastity" } },
            {"Coeds", new string[] { "college", "university", "coed" } },
            {"Colored Hair", new string[] { "hair color", "other hair color" } },
            {"Cop", new string[] { "police" } },
            {"Couple", new string[] { "couples" } },
            {"Creampie", new string[] { "cream pie", "creampie compilation" } },
            {"Cuckold", new string[] { "brutal cuckolding", "cuckolding" } },
            {"Cum In Mouth", new string[] { "cum-in-mouth" } },
            {"Cum Shot", new string[] { "cumshot" } },
            {"Cum Swallowing", new string[] { "cum swallow", "swallow cum", "swallowing cum", "cum swallowers" } },
            {"Cum Swap", new string[] { "cumswap", "cum swapping" } },
            {"Curly Hair", new string[] { "curly" } },
            {"Curvy", new string[] { "thick", "voluptuous", "curvy woman" } },
            {"Deep Throat", new string[] { "deepthroat", "deepthroating" } },
            {"Dildo", new string[] { "dildo play" } },
            {"Dirty Talk", new string[] { "dirty talking" } },
            {"Double Penetration (DP)", new string[] { "dp", "double penetration", "double penetraton (dp)" } },
            {"Ebony", new string[] { "black", "black girl", "dark skin", "african american" } },
            {"Electro Play", new string[] { "electrical play", "electrode punishments" } },
            {"European", new string[] { "euro", "europe" } },
            {"Face Fucking", new string[] { "face fuck", "facefucking" } },
            {"Face Sitting", new string[] { "facesitting", "queening" } },
            {"Facial", new string[] { "facial (multiple)", "facial (pov)", "cumshot facial", "open mouth facial", "facials", "facial - pov" } },
            {"Fair Skin", new string[] { "pale", "pale skin", "whiteskin" } },
            {"Fake Tits", new string[] { "enhanced", "enhanced tits", "silicone tits", "fake boobs" } },
            {"Feet", new string[] { "foot", "barefeet" } },
            {"Fingering", new string[] { "finger fucking" } },
            {"First Anal", new string[] { "first time anal" } },
            {"First Appearance", new string[] { "first time porn", "first porn shoot", "debut", "model debut" } },
            {"First Interracial", new string[] { "first time ir" } },
            {"Fishnets", new string[] { "fishnet", "fishnet stockings" } },
            {"Fisting", new string[] { "fisting's finest" } },
            {"Foot Job", new string[] { "footjobs", "footjob" } },
            {"Gangbang", new string[] { "gangbangs" } },
            {"Gaping", new string[] { "gape", "gaping playlist", "gaping", "gaped ass" } },
            {"Girl-Girl Pissing", new string[] { "lesbian pissing", "girl girl pissing" } },
            {"Girl-Girl", new string[] { "girl-on-girl", "girl on girl", "girl girl" } },
            {"Girlfriend Experience", new string[] { "gfe" } },
            {"Glamorous", new string[] { "glamour" } },
            {"Group Sex", new string[] { "group" } },
            {"Gym", new string[] { "in the gym", "gym selfie porn" } },
            {"Hairy Pussy", new string[] { "hairy", "hairy bush", "bush" } },
            {"Hand Job", new string[] { "handjob", "handjob (pov)", "handjobs", "handjob - pov" } },
            {"Hardcore", new string[] { "hardcore sex" } },
            {"Heterosexual", new string[] { "straight", "straight porn" } },
            {"High Heels", new string[] { "heels" } },
            {"Holidays", new string[] { "holidayparty" } },
            {"Hood", new string[] { "hooded" } },
            {"Hotel Room", new string[] { "hotel" } },
            {"Housewife", new string[] { "housewives" } },
            {"Indoors", new string[] { "indoor" } },
            {"Interview", new string[] { "interviews" } },
            {"Jerk Off Instructions (JOI)", new string[] { "jerk off instruction", "joi", "joi games" } },
            {"Landing Strip", new string[] { "landing strip pussy" } },
            {"Latex", new string[] { "latex darlings" } },
            {"Latina", new string[] { "latinas" } },
            {"Lesbian", new string[] { "lesbians" } },
            {"Lezdom", new string[] { "lesbian domination" } },
            {"Lingerie", new string[] { "lenceria" } },
            {"Maid", new string[] { "maid fetish" } },
            {"Mask", new string[] { "masks" } },
            {"Massage", new string[] { "body massage" } },
            {"Masturbation", new string[] { "masturbacion" } },
            {"Medical Fetish", new string[] { "nurse", "doctor", "nurse play", "doctor/nurse" } },
            {"Medium Tits", new string[] { "medium boobs" } },
            {"Milf & Mature", new string[] { "mature & milf" } },
            {"Milf", new string[] { "milfs" } },
            {"Mini Skirt", new string[] { "miniskirts" } },
            {"Model", new string[] { "modelo" } },
            {"Muscular", new string[] { "muscle" } },
            {"Natural Tits", new string[] { "natural boobs", "real tits" } },
            {"Nerd", new string[] { "nerdy" } },
            {"Nude", new string[] { "completely naked", "naked" } },
            {"Office Setting", new string[] { "office" } },
            {"Oil", new string[] { "oiled", "babyoil" } },
            {"Older/Younger", new string[] { "older / younger" } },
            {"One-On-One", new string[] { "1 on 1" } },
            {"Oral Sex", new string[] { "oral" } },
            {"Orgasms", new string[] { "orgasm", "amazing orgasms" } },
            {"Orgy", new string[] { "four, more" } },
            {"Outdoor Sex", new string[] { "outdoors" } },
            {"Outdoors", new string[] { "outdoor" } },
            {"Paddle", new string[] { "paddling" } },
            {"Panties", new string[] { "braguitas" } },
            {"Pantyhose & Stockings", new string[] { "nude stockings", "stockings", "nylons stockings", "pantyhose", "pantyhose footjobs", "black stockings", "pantyhose / stockings", "stocking" } },
            {"Piercing", new string[] { "piercings" } },
            {"Pile Driver", new string[] { "pile driving" } },
            {"Piss Play", new string[] { "pee", "pissing" } },
            {"Pool", new string[] { "poolparty" } },
            {"Pornstar Experience", new string[] { "pse" } },
            {"Pornstar", new string[] { "pornstars" } },
            {"Pov", new string[] { "p.o.v.", "femdom pov" } },
            {"Pussy Eating", new string[] { "pussy lick", "pussy licking", "cunilingus" } },
            {"Reality Porn", new string[] { "reality" } },
            {"Redhead", new string[] { "red head", "red heads" } },
            {"Rim Job", new string[] { "rimming", "rimjob" } },
            {"Schoolgirl Outfit", new string[] { "school girl", "schoolgirl" } },
            {"Sci-Fi and Fantasy", new string[] { "sci fi" } },
            {"Sex", new string[] { "sexo" } },
            {"Shaven Pussy", new string[] { "shaved pussy", "bald pussy", "shaved" } },
            {"Shoe Fetish", new string[] { "sneaker fetish", "shoeplay" } },
            {"Sissification", new string[] { "sissy training course" } },
            {"Skirt", new string[] { "skirts" } },
            {"Small Ass", new string[] { "small booty", "small butt" } },
            {"Small Tits", new string[] { "small boobs" } },
            {"Solo", new string[] { "solo masturbation", "solo sex", "solo action" } },
            {"Squirting", new string[] { "squirt", "top squirting videos" } },
            {"Step Daughter", new string[] { "stepdaughter" } },
            {"Step Mom", new string[] { "stepmom" } },
            {"Step sister" , new string[] { "stepsister", "stepsis", "step sis", "ste sister", "step siter", "step-sister" } },
            {"Strap-On", new string[] { "strap on", "pov strapon" } },
            {"Striptease", new string[] { "strip tease" } },
            {"Swallowing", new string[] { "swallow", "amateur swallow" } },
            {"Tan Lines", new string[] { "tanlines" } },
            {"Tan", new string[] { "tanned skin" } },
            {"Tattoo", new string[] { "tattoos", "tattoo girl", "tattooed" } },
            {"Teacher Fetish", new string[] { "teacher" } },
            {"Tease And Denial", new string[] { "tease & denial" } },
            {"Teen", new string[] { "teens", "teen role", "bad teens punished", "teen porn", "18+ teen" } },
            {"Thong", new string[] { "thongs" } },
            {"Threesome", new string[] { "3some", "3 way", "2-on-1", "2on1", "2 on 1", "threesomes" } },
            {"Titty Fuck", new string[] { "tittyfuck (pov)", "tittyfuck", "tit fuck", "titty fucking", "tittyfuck - pov", "tit fucking" } },
            {"Toys", new string[] { "sex toys", "toy insertions" } },
            {"Transsexual", new string[] { "ts", "transexual", "trans" } },
            {"Tribbing", new string[] { "scissoring" } },
            {"Trimmed Pussy", new string[] { "trimmed", "trimmed bush" } },
            {"Uncircumcised", new string[] { "uncut dicks" } },
            {"Uniform", new string[] { "uniforms" } },
            {"Valentine's Day", new string[] { "valentines day" } },
            {"Whipping", new string[] { "whip" } },

            // Positions
            {"69 (Position)", new string[] { "sixty-nine", "69", "69 position" } },
            {"Cowgirl (Position)", new string[] { "cow girl", "cowgirl", "cowgirl (pov)" } },
            {"Missionary (Position)", new string[] { "missionary", "missionary (pov)", "missionary - pov" } },
            {"Reverse Cowgirl (Position)", new string[] { "reverse cow girl", "reverse cowgirl", "reverse cowgirl (pov)", "cowgirl - pov" } },
        };
    }
}
