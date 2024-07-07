using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PhoenixAdult.Configuration;

#if __EMBY__
#else
// using Sentry;
#endif

namespace PhoenixAdult.Helpers.Utils
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Temp")]
    public struct AnalyticsExeption
    {
        public string Request { get; set; }

        public int[] SiteNum { get; set; }

        public string SearchTitle { get; set; }

        public DateTime? SearchDate { get; set; }

        public string ProviderName { get; set; }

        public Exception Exception { get; set; }
    }

    internal static class Analytics
    {
        public static readonly string LogsPath = Path.Combine(Plugin.Instance.DataFolderPath, "logs");

        private static AnalyticsStructure AnalyticsData { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "For future")]

        public static async Task Send(AnalyticsExeption exception, CancellationToken cancellationToken)
        {
            AnalyticsData = new AnalyticsStructure
            {
                User = new UserStructure
                {
                    DateTime = DateTime.UtcNow,
                    ServerPlatform = Consts.PluginInstance,
                    PluginVersion = Consts.PluginVersion,
                    Options = Plugin.Instance.Configuration,
                },
                Info = new InfoStructure
                {
                    Request = exception.Request,
                    SiteNum = exception.SiteNum != null ? $"{exception.SiteNum[0]}#{exception.SiteNum[1]}" : null,
                    SiteName = exception.SiteNum != null ? Helper.GetSearchSiteName(exception.SiteNum) : null,
                    SearchTitle = exception.SearchTitle,
                    SearchDate = exception.SearchDate.HasValue ? exception.SearchDate.Value.ToString("yyyy-MM-dd") : null,
                    ProviderName = exception.ProviderName,
                },
                Error = new ErrorStructure
                {
                    Name = exception.Exception.Message,
                    Text = exception.Exception.StackTrace,
                },
            };

            if (!Plugin.Instance.Configuration.DisableAnalytics)
            {
#if __EMBY__
#else
                /*
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
                SentrySdk.CaptureException(exception.Exception);
                */
#endif
            }

            if (Plugin.Instance.Configuration.EnableDebug)
            {
                if (!Directory.Exists(LogsPath))
                {
                    Logger.Info($"Creating analytics directory \"{LogsPath}\"");
                    Directory.CreateDirectory(LogsPath);
                }

                var fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfffffff")}.json.gz";
                fileName = Path.Combine(LogsPath, fileName);

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
