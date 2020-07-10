using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PhoenixAdult.Providers.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public class PhoenixAdultProvider : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => "PhoenixAdult";

        public static string PluginName;
        public static ILogger Log;
        public static IHttpClient Http;

        public PhoenixAdultProvider(ILoggerFactory log, IHttpClient http)
        {
            PluginName = Name;
            if (log != null)
                Log = log.CreateLogger(Name);
            Http = http;

            int siteListCount = 0;
            foreach (var site in PhoenixAdultList.SiteList.Values)
                siteListCount += site.Count;

            var siteModuleList = new List<string>();
            for (int i = 0; i < PhoenixAdultList.SiteList.Count; i += 1)
            {
                var siteModule = PhoenixAdultList.GetProviderBySiteID(i);
                if (siteModule != null && !siteModuleList.Contains(siteModule.ToString()))
                    siteModuleList.Add(siteModule.ToString());
            }

            int actressListCount = 0;
            foreach (var actress in PhoenixAdultPeoples.ReplaceListStudio.Values)
                actressListCount += actress.Count;
            actressListCount += PhoenixAdultPeoples.ReplaceList.Count;

            Log.LogInformation($"Plugin version: {Plugin.Instance.Version}");
            Log.LogInformation($"Number of supported sites: {siteListCount}");
            Log.LogInformation($"Number of site modules: {siteModuleList.Count}");
            Log.LogInformation($"Number of site aliases: {_abbrieviationList.Count}");
            Log.LogInformation($"Number of actress: {actressListCount}");
            Log.LogInformation($"Default plugin locale: {PhoenixAdultHelper.Lang.Name}");
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            List<RemoteSearchResult> result = new List<RemoteSearchResult>();

            if (searchInfo == null)
                return result;

            Log.LogInformation($"searchInfo.Name: {searchInfo.Name}");

            var title = ReplaceAbbrieviation(searchInfo.Name);
            var site = GetSiteFromTitle(title);
            if (site.Key != null)
            {
                string searchTitle = GetClearTitle(title, site.Value),
                       searchDate = string.Empty,
                       encodedTitle;
                DateTime? searchDateObj;
                var titleAfterDate = GetDateFromTitle(searchTitle);

                var siteNum = new int[2] {
                    site.Key[0],
                    site.Key[1]
                };
                searchTitle = titleAfterDate.Item1;
                searchDateObj = titleAfterDate.Item2;
                if (searchDateObj.HasValue)
                    searchDate = searchDateObj.Value.ToString("yyyy-MM-dd", PhoenixAdultHelper.Lang);
                encodedTitle = Uri.EscapeDataString(searchTitle);

                Log.LogInformation($"site: {siteNum[0]}:{siteNum[1]} ({site.Value})");
                Log.LogInformation($"searchTitle: {searchTitle}");
                Log.LogInformation($"encodedTitle: {encodedTitle}");
                Log.LogInformation($"searchDate: {searchDate}");

                var provider = PhoenixAdultList.GetProviderBySiteID(siteNum[0]);
                if (provider != null)
                {
                    result = await provider.Search(siteNum, searchTitle, encodedTitle, searchDateObj, cancellationToken).ConfigureAwait(false);
                    if (result.Count > 0)
                        if (result.Any(scene => scene.IndexNumber.HasValue))
                            result = result.OrderByDescending(scene => scene.IndexNumber.HasValue).ThenBy(scene => scene.IndexNumber).ToList();
                        else if (!string.IsNullOrEmpty(searchDate) && result.All(scene => scene.PremiereDate.HasValue) && result.Any(scene => scene.PremiereDate.Value != searchDateObj))
                            result = result.OrderBy(scene => Math.Abs((searchDateObj - scene.PremiereDate).Value.TotalDays)).ToList();
                        else
                            result = result.OrderByDescending(scene => 100 - PhoenixAdultHelper.LevenshteinDistance(searchTitle, scene.Name)).ToList();
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>
            {
                HasMetadata = false,
                Item = new Movie()
            };

            if (info == null)
                return result;

            var sceneID = info.ProviderIds;
            if (!sceneID.ContainsKey(Name))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                    sceneID = searchResults.First().ProviderIds;
            }

            var externalID = sceneID.GetValueOrDefault(Name);
            if (string.IsNullOrEmpty(externalID))
                return result;

            var curID = externalID.Split('#');
            if (curID.Length < 3)
                return result;

            var provider = PhoenixAdultList.GetProviderBySiteID(int.Parse(curID[0], PhoenixAdultHelper.Lang));
            if (provider != null)
            {
                Log.LogInformation($"PhoenixAdult ID: {externalID}");
                result = await provider.Update(curID, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    result.HasMetadata = true;
                    result.Item.OfficialRating = "XXX";
                    result.Item.ProviderIds = sceneID;

                    if ((result.People != null) && result.People.Any())
                        result.People = PhoenixAdultPeoples.Cleanup(result);
                    if (result.Item.Genres != null && result.Item.Genres.Any())
                        result.Item.Genres = PhoenixAdultGenres.Cleanup(result.Item.Genres, result.Item.Name);
                }
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken) => PhoenixAdultHelper.GetImageResponse(url, cancellationToken);

        public static KeyValuePair<int[], string> GetSiteFromTitle(string title)
        {
            string clearName = Regex.Replace(title, @"\W", string.Empty);
            var possibleSites = new Dictionary<int[], string>();

            foreach (var site in PhoenixAdultList.SiteList)
                foreach (var siteData in site.Value)
                {
                    string clearSite = Regex.Replace(siteData.Value[0], @"\W", string.Empty);
                    if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                        possibleSites.Add(new int[] { site.Key, siteData.Key }, clearSite);
                }

            if (possibleSites.Count > 0)
                return possibleSites.OrderByDescending(x => x.Value.Length).First();

            return new KeyValuePair<int[], string>(null, null);
        }

        public static string GetClearTitle(string title, string siteName)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            string clearName = PhoenixAdultHelper.Lang.TextInfo.ToTitleCase(title),
                   clearSite = siteName;

            clearName = clearName.Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

            clearName = Regex.Replace(clearName, @"[^a-zA-Z0-9 ]", " ");
            clearSite = Regex.Replace(clearSite, @"\W", string.Empty);

            bool matched = false;
            while (clearName.Contains(' ', StringComparison.OrdinalIgnoreCase))
            {
                clearName = PhoenixAdultHelper.ReplaceFirst(clearName, " ", string.Empty);
                if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                clearName = clearName.Replace(clearSite, string.Empty, StringComparison.OrdinalIgnoreCase);
                clearName = string.Join(" ", clearName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            return clearName;
        }

        public static (string, DateTime?) GetDateFromTitle(string title)
        {
            string searchDate,
                   searchTitle = title;
            var regExRules = new Dictionary<string, string> {
                { @"\b\d{4} \d{2} \d{2}\b", "yyyy MM dd" },
                { @"\b\d{2} \d{2} \d{2}\b", "yy MM dd" }
            };
            (string, DateTime?) searchData = (searchTitle, null);

            foreach (var regExRule in regExRules)
            {
                var regEx = Regex.Match(searchTitle, regExRule.Key);
                if (regEx.Groups.Count > 0)
                    if (DateTime.TryParseExact(regEx.Groups[0].Value, regExRule.Value, PhoenixAdultHelper.Lang, DateTimeStyles.None, out DateTime searchDateObj))
                    {
                        searchDate = searchDateObj.ToString("yyyy-MM-dd", PhoenixAdultHelper.Lang);
                        searchTitle = Regex.Replace(searchTitle, regExRule.Key, string.Empty).Trim();

                        searchData = (searchTitle, searchDateObj);
                        break;
                    }
            }

            return searchData;
        }

        public static string ReplaceAbbrieviation(string title)
        {
            string newTitle = title;

            foreach (var abbrieviation in _abbrieviationList)
            {
                Regex regex = new Regex(abbrieviation.Key, RegexOptions.IgnoreCase);
                if (regex.IsMatch(title))
                {
                    newTitle = regex.Replace(title, abbrieviation.Value, 1);
                    break;
                }
            }

            return newTitle;
        }

        private static readonly Dictionary<string, string> _abbrieviationList = new Dictionary<string, string> {
            { @"^18og ", "18OnlyGirls " },
            { @"^18yo ", "18YearsOld " },
            { @"^1kf ", "1000Facials " },
            { @"^21ea ", "21EroticAnal " },
            { @"^21fa ", "21FootArt " },
            { @"^21n ", "21Naturals " },
            { @"^2cst ", "2ChicksSameTime " },
            { @"^a1o1 ", "Asian1on1 " },
            { @"^aa ", "AmateurAllure " },
            { @"^ad ", "AmericanDaydreams " },
            { @"^add ", "ManualAddActors " },
            { @"^agm ", "AllGirlMassage " },
            { @"^am ", "AssMasterpiece " },
            { @"^analb ", "AnalBeauty " },
            { @"^ap ", "AssParade " },
            { @"^baebz ", "Baeb " },
            { @"^bblib ", "BigButtsLikeItBig " },
            { @"^bcasting ", "BangCasting " },
            { @"^bcb ", "BigCockBully " },
            { @"^bch ", "BigCockHero " },
            { @"^bconfessions ", "BangConfessions " },
            { @"^bdpov ", "BadDaddyPOV " },
            { @"^bex ", "BrazzersExxtra " },
            { @"^bgb ", "BabyGotBoobs " },
            { @"^bgbs ", "BoundGangbangs " },
            { @"^bglamkore ", "BangGlamkore " },
            { @"^bgonzo ", "BangGonzo " },
            { @"^bin ", "BigNaturals " },
            { @"^bjf ", "BlowjobFridays " },
            { @"^bp ", "ButtPlays " },
            { @"^brealteens ", "BangRealTeens " },
            { @"^btas ", "BigTitsatSchool " },
            { @"^btaw ", "BigTitsatWork " },
            { @"^btc", "BigTitCreampie " },
            { @"^btis ", "BigTitsinSports " },
            { @"^btiu ", "BigTitsinUniform " },
            { @"^btlbd ", "BigTitsLikeBigDicks " },
            { @"^btra ", "BigTitsRoundAsses " },
            { @"^burna ", "BurningAngel " },
            { @"^bwb ", "BigWetButts " },
            { @"^clip ", "LegalPorno " },
            { @"^cps ", "CherryPimps " },
            { @"^css ", "CzechStreets " },
            { @"^cuf ", "CumFiesta " },
            { @"^cws ", "CzechWifeSwap " },
            { @"^da ", "DoctorAdventures " },
            { @"^daughter ", "DaughterSwap " },
            { @"^daughters ", "DaughterSwap " },
            { @"^dbm ", "DontBreakMe " },
            { @"^dc ", "DorcelVision " },
            { @"^ddfb ", "DDFBusty " },
            { @"^dm ", "DirtyMasseur " },
            { @"^dnj ", "DaneJones " },
            { @"^dpg ", "DigitalPlayground " },
            { @"^dsw ", "DaughterSwap " },
            { @"^dwc ", "DirtyWivesClub " },
            { @"^dwp ", "DayWithAPornstar " },
            { @"^esp ", "EuroSexParties " },
            { @"^ete ", "EuroTeenErotica " },
            { @"^ext ", "ExxxtraSmall " },
            { @"^fams ", "FamilyStrokes " },
            { @"^faq ", "FirstAnalQuest " },
            { @"^fds ", "FakeDrivingSchool " },
            { @"^fft ", "FemaleFakeTaxi " },
            { @"^fhd ", "FantasyHD " },
            { @"^fhl ", "FakeHostel " },
            { @"^fho ", "FakehubOriginals " },
            { @"^fka ", "FakeAgent " },
            { @"^fm ", "FuckingMachines " },
            { @"^fms ", "FantasyMassage " },
            { @"^frs ", "FitnessRooms " },
            { @"^ft ", "FastTimes " },
            { @"^ftx ", "FakeTaxi " },
            { @"^gbcp ", "GangbangCreampie " },
            { @"^gft ", "GrandpasFuckTeens " },
            { @"^gta ", "GirlsTryAnal " },
            { @"^gw ", "GirlsWay " },
            { @"^h1o1 ", "Housewife1on1 " },
            { @"^ham ", "HotAndMean " },
            { @"^hart ", "Hegre " },
            { @"^hcm ", "HotCrazyMess " },
            { @"^hegre-art ", "Hegre " },
            { @"^hoh ", "HandsOnHardcore " },
            { @"^hotab ", "HouseofTaboo " },
            { @"^ht ", "Hogtied " },
            { @"^hustl3r ", "Hustler " },
            { @"^ihaw ", "IHaveAWife " },
            { @"^iktg ", "IKnowThatGirl " },
            { @"^il ", "ImmoralLive " },
            { @"^kha ", "KarupsHA " },
            { @"^kow ", "KarupsOW " },
            { @"^kpc ", "KarupsPC " },
            { @"^la ", "LatinAdultery " },
            { @"^latn ", "LookAtHerNow " },
            { @"^lcd ", "LittleCaprice " },
            { @"^lhf ", "LoveHerFeet " },
            { @"^lsb ", "Lesbea " },
            { @"^lst ", "LatinaSexTapes " },
            { @"^lta ", "LetsTryAnal " },
            { @"^maj ", "ManoJob " },
            { @"^mbb ", "MommyBlowsBest " },
            { @"^mbt ", "MomsBangTeens " },
            { @"^mc ", "MassageCreep " },
            { @"^mcu ", "MonsterCurves " },
            { @"^mdhf ", "MyDaughtersHotFriend " },
            { @"^mdhg ", "MyDadsHotGirlfriend " },
            { @"^mfa ", "ManuelFerrara " },
            { @"^mfhg ", "MyFriendsHotGirl " },
            { @"^mfhm ", "MyFriendsHotMom " },
            { @"^mfl ", "Mofos " },
            { @"^mfst ", "MyFirstSexTeacher " },
            { @"^mgb ", "MommyGotBoobs " },
            { @"^mgbf ", "MyGirlfriendsBustyFriend " },
            { @"^mic ", "MomsInControl " },
            { @"^mih ", "MilfHunter " },
            { @"^mj ", "ManoJob " },
            { @"^mlib ", "MilfsLikeItBig " },
            { @"^mlt ", "MomsLickTeens " },
            { @"^mmgs ", "MommysGirl " },
            { @"^mmts ", "MomsTeachSex " },
            { @"^mnm ", "MyNaughtyMassage " },
            { @"^mom ", "MomXXX " },
            { @"^mpov ", "MrPOV " },
            { @"^mr ", "MassageRooms " },
            { @"^mrs ", "MassageRooms " },
            { @"^mshf ", "MySistersHotFriend " },
            { @"^mts ", "MomsTeachSex " },
            { @"^mvft ", "MyVeryFirstTime " },
            { @"^mwhf ", "MyWifesHotFriend " },
            { @"^na ", "NaughtyAthletics " },
            { @"^naf ", "NeighborAffair " },
            { @"^nam ", "NaughtyAmerica " },
            { @"^nb ", "NaughtyBookworms " },
            { @"^news ", "NewSensations " },
            { @"^nf ", "NubileFilms " },
            { @"^no ", "NaughtyOffice " },
            { @"^nrg ", "NaughtyRichGirls " },
            { @"^nubilef ", "NubileFilms " },
            { @"^num ", "NuruMassage " },
            { @"^nw ", "NaughtyWeddings " },
            { @"^obj ", "OnlyBlowjob " },
            { @"^otb ", "OnlyTeenBlowjobs " },
            { @"^pav ", "PixAndVideo " },
            { @"^pba ", "PublicAgent " },
            { @"^pc ", "PrincessCum " },
            { @"^pdmcl ", "ChicasLoca " },
            { @"^pf ", "PornFidelity " },
            { @"^phd ", "PassionHD " },
            { @"^phdp ", "PetiteHDPorn " },
            { @"^plib ", "PornstarsLikeitBig " },
            { @"^pop ", "PervsOnPatrol " },
            { @"^ppu ", "PublicPickups " },
            { @"^prdi ", "PrettyDirty " },
            { @"^ps ", "PropertySex " },
            { @"^ptt ", "Petite " },
            { @"^pud ", "PublicDisgrace " },
            { @"^reg ", "RealExGirlfriends " },
            { @"^rkp ", "RKPrime " },
            { @"^rws ", "RealWifeStories " },
            { @"^saf ", "ShesAFreak " },
            { @"^sart ", "SexArt " },
            { @"^sas ", "SexandSubmission " },
            { @"^sbj ", "StreetBlowjobs " },
            { @"^sins ", "SinsLife " },
            { @"^sislove ", "SisLovesMe " },
            { @"^smb ", "ShareMyBF " },
            { @"^ssc ", "StepSiblingsCaught " },
            { @"^ssn ", "ShesNew " },
            { @"^sts ", "StrandedTeens " },
            { @"^swsn ", "SwallowSalon " },
            { @"^tdp ", "TeensDoPorn " },
            { @"^tds ", "TheDickSuckers " },
            { @"^ted ", "Throated " },
            { @"^tf ", "TeenFidelity " },
            { @"^tgs ", "ThisGirlSucks " },
            { @"^these ", "TheStripperExperience " },
            { @"^tla ", "TeensLoveAnal " },
            { @"^tlc ", "TeensLoveCream " },
            { @"^tle ", "TheLifeErotic " },
            { @"^tlhc ", "TeensLoveHugeCocks " },
            { @"^tlib ", "TeensLikeItBig " },
            { @"^tlm ", "TeensLoveMoney " },
            { @"^tog ", "TonightsGirlfriend " },
            { @"^togc ", "TonightsGirlfriendClassic " },
            { @"^tspa ", "TrickySpa " },
            { @"^tss ", "ThatSitcomShow " },
            { @"^tuf ", "TheUpperFloor " },
            { @"^wa ", "WhippedAss " },
            { @"^wfbg ", "WeFuckBlackGirls " },
            { @"^wkp ", "Wicked " },
            { @"^wlt ", "WeLiveTogether " },
            { @"^woc ", "WildOnCam " },
            { @"^wov ", "WivesOnVacation " },
            { @"^wowg ", "WowGirls " },
            { @"^wy ", "WebYoung " },
            { @"^ztod ", "ZeroTolerance " },
            { @"^zzs ", "ZZseries " },
        };
    }
}
