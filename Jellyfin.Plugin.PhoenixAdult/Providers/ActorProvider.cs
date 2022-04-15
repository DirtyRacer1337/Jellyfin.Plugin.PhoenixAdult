using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

#if __EMBY__
using MediaBrowser.Common.Net;
#else
using System.Net.Http;
#endif

namespace PhoenixAdult.Providers
{
    public class ActorProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null || searchInfo.ProviderIds.Any(o => !string.IsNullOrEmpty(o.Value)))
            {
                return result;
            }

            var providerList = new List<string> { "Freeones" };

            foreach (var siteName in providerList)
            {
                var title = $"{siteName} {searchInfo.Name}";
                var site = Helper.GetSiteFromTitle(title);
                string actorName = Helper.GetClearTitle(title, site.siteName);

                Logger.Info($"site: {site.siteNum[0]}:{site.siteNum[1]} ({site.siteName})");
                Logger.Info($"actorName: {actorName}");

                DateTime? searchDateObj = null;
                if (searchInfo.PremiereDate.HasValue)
                {
#if __EMBY__
                    searchDateObj = searchInfo.PremiereDate.Value.DateTime;
#else
                    searchDateObj = searchInfo.PremiereDate.Value;
#endif
                }

                var provider = Helper.GetProviderBySiteID(site.siteNum[0]);
                if (provider != null)
                {
                    Logger.Info($"provider: {provider}");

                    try
                    {
                        result = await provider.Search(site.siteNum, actorName, searchDateObj, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Actor Search error: \"{e}\"");

                        await Analytics.Send(
                            new AnalyticsExeption
                            {
                                Request = title,
                                SiteNum = site.siteNum,
                                SearchTitle = actorName,
                                ProviderName = provider.ToString(),
                                Exception = e,
                            }, cancellationToken).ConfigureAwait(false);
                    }

                    if (result.Any())
                    {
                        foreach (var scene in result)
                        {
                            scene.ProviderIds[this.Name] = $"{site.siteNum[0]}#{site.siteNum[1]}#" + scene.ProviderIds[this.Name];
                            scene.Name = scene.Name.Trim();
                        }

                        result = result.OrderByDescending(o => 100 - LevenshteinDistance.Calculate(searchInfo.Name, o.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    break;
                }
            }

            return result;
        }

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>()
            {
                HasMetadata = false,
                Item = new Person(),
            };

            if (info == null)
            {
                return result;
            }

            string[] curID = null;
            var sceneID = info.ProviderIds;
            if (sceneID.TryGetValue(this.Name, out var externalID))
            {
                curID = externalID.Split('#');
            }

            if ((!sceneID.ContainsKey(this.Name) || curID == null || curID.Length < 3) && !Plugin.Instance.Configuration.DisableAutoIdentify)
            {
                var searchResults = await this.GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    sceneID = searchResults.First().ProviderIds;

                    sceneID.TryGetValue(this.Name, out externalID);
                    curID = externalID.Split('#');
                }
            }

            if (curID == null)
            {
                return result;
            }

            var siteNum = new int[2] { int.Parse(curID[0], CultureInfo.InvariantCulture), int.Parse(curID[1], CultureInfo.InvariantCulture) };

            var provider = Helper.GetProviderBySiteID(siteNum[0]);
            if (provider != null)
            {
                Logger.Info($"PhoenixAdult Actor ID: {externalID}");

                MetadataResult<BaseItem> res = null;
                try
                {
                    res = await provider.Update(siteNum, curID.Skip(2).ToArray(), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"Actor Update error: \"{e}\"");

                    await Analytics.Send(
                        new AnalyticsExeption
                        {
                            Request = externalID,
                            SearchTitle = info.Name,
                            ProviderName = provider.ToString(),
                            Exception = e,
                        }, cancellationToken).ConfigureAwait(false);
                }

                if (res != null)
                {
                    result.HasMetadata = true;
                    result.Item = (Person)res.Item;
                }

                if (result.HasMetadata)
                {
                    result.Item.ProviderIds.Update(this.Name, sceneID[this.Name]);
                    result.Item.ProviderIds.Update(this.Name + "URL", result.Item.ExternalId);

                    result.Item.OriginalTitle = WebUtility.HtmlDecode(result.Item.OriginalTitle);
                    var aliases = result.Item.OriginalTitle.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var newAliases = new List<string>();
                    foreach (var name in aliases)
                    {
                        var actorName = name.Trim();

                        if (!string.IsNullOrEmpty(actorName) && !newAliases.Contains(actorName, StringComparer.Ordinal))
                        {
                            newAliases.Add(actorName);
                        }
                    }

                    var bornPlaceList = new List<string>();
                    if (result.Item.ProductionLocations.Any())
                    {
                        foreach (var bornPlace in result.Item.ProductionLocations[0].Split(","))
                        {
                            var location = bornPlace.Trim();

                            if (!string.IsNullOrEmpty(location))
                            {
                                bornPlaceList.Add(location);
                            }
                        }
                    }

                    result.Item.ProductionLocations = new string[] { string.Join(", ", bornPlaceList) };

                    if (!newAliases.Contains(info.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        result.HasMetadata = false;
                    }

                    result.Item.OriginalTitle = string.Join(", ", newAliases);

                    if (!string.IsNullOrEmpty(result.Item.Overview))
                    {
                        result.Item.Overview = HttpUtility.HtmlDecode(result.Item.Overview).Trim();
                    }

                    if (result.Item.PremiereDate.HasValue)
                    {
                        result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                    }
                }
            }

            return result;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
#endif
        {
            return Helper.GetImageResponse(url, cancellationToken);
        }
    }
}
