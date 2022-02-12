using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PhoenixAdult.Configuration;
using Sentry;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class Analytics
    {
        public static readonly string LogsPath = Path.Combine(Plugin.Instance.DataFolderPath, "logs");

        private static AnalyticsStructure AnalyticsData { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "For future")]

        public static async Task Send(string request, int[] siteNum, string siteName, string searchTitle, DateTime? searchDate, string providerName, Exception e, CancellationToken cancellationToken)
        {
            if (!Plugin.Instance.Configuration.DisableAnalytics)
            {
                if (!Directory.Exists(LogsPath))
                {
                    Logger.Info($"Creating analytics directory \"{LogsPath}\"");
                    Directory.CreateDirectory(LogsPath);
                }

                var fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfffffff")}.json.gz";
                fileName = Path.Combine(LogsPath, fileName);

#if __EMBY__
                var pluginFullName = "Emby.Plugins.PhoenixAdult";
#else
                var pluginFullName = "Jellyfin.Plugin.PhoenixAdult";
#endif

                AnalyticsData = new AnalyticsStructure
                {
                    User = new UserStructure
                    {
                        DateTime = DateTime.UtcNow,
                        ServerPlatform = pluginFullName,
                        PluginVersion = $"{Plugin.Instance.Version.ToString()} build {Helper.GetLinkerTime(Assembly.GetAssembly(typeof(Plugin)))}",
                        Options = Plugin.Instance.Configuration,
                    },
                    Info = new InfoStructure
                    {
                        Request = request,
                        SiteNum = siteNum != null ? $"{siteNum[0]}#{siteNum[1]}" : null,
                        SiteName = siteName,
                        SearchTitle = searchTitle,
                        SearchDate = searchDate.HasValue ? searchDate.Value.ToString("yyyy-MM-dd") : null,
                        ProviderName = providerName,
                    },
                    Error = new ErrorStructure
                    {
                        Name = e.Message,
                        Text = e.StackTrace,
                    },
                };

                SentrySdk.ConfigureScope(scope =>
                {
                    scope.User = new User()
                    {
                        Id = Plugin.Instance.Configuration.UID,
                    };
                    scope.Release = AnalyticsData.User.PluginVersion;
                    scope.Environment = AnalyticsData.User.ServerPlatform;
                    scope.Contexts["Options"] = AnalyticsData.User.Options;
                    scope.Contexts["Info"] = AnalyticsData.Info;
                });
                SentrySdk.CaptureException(e);

                var json = JsonConvert.SerializeObject(AnalyticsData, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                });
                var bytes = Encoding.UTF8.GetBytes(json);

                using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate))
                using (GZipStream zipStream = new GZipStream(fs, CompressionMode.Compress, false))
                {
                    zipStream.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public struct AnalyticsStructure
        {
            public UserStructure User { get; set; }

            public InfoStructure Info { get; set; }

            public ErrorStructure Error { get; set; }
        }

        public struct UserStructure
        {
            public string UID { get; set; }

            public DateTime DateTime { get; set; }

            public string ServerPlatform { get; set; }

            public string PluginVersion { get; set; }

            public PluginConfiguration Options { get; set; }
        }

        public struct InfoStructure
        {
            public string Request { get; set; }

            public string SiteNum { get; set; }

            public string SiteName { get; set; }

            public string SearchTitle { get; set; }

            public string SearchDate { get; set; }

            public string ProviderName { get; set; }
        }

        public struct ErrorStructure
        {
            public string Name { get; set; }

            public string Text { get; set; }
        }
    }
}
