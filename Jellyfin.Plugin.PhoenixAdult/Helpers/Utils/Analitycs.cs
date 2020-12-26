using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PhoenixAdult.Configuration;

namespace PhoenixAdult.Helpers.Utils
{
    internal static class Analitycs
    {
        public static readonly string LogsPath = Path.Combine(Plugin.Instance.DataFolderPath, "logs");

        private static AnalitycsStructure AnalitycsData { get; set; }

        public static async Task Send(string request, int[] siteNum, string siteName, string searchTitle, string searchDate, string providerName, Exception e, CancellationToken cancellationToken)
        {
            if (!Plugin.Instance.Configuration.DisableAnalytics)
            {
                if (!Directory.Exists(LogsPath))
                {
                    Logger.Info($"Creating analitycs directory \"{LogsPath}\"");
                    Directory.CreateDirectory(LogsPath);
                }

                var fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfffffff")}.json.gz";
                fileName = Path.Combine(LogsPath, fileName);

                AnalitycsData = new AnalitycsStructure
                {
                    User = new UserStructure
                    {
                        DateTime = DateTime.UtcNow,
                        Options = Plugin.Instance.Configuration,
                    },
                    Info = new InfoStructure
                    {
                        Request = request,
                        SiteNum = siteNum != null ? $"{siteNum[0]}#{siteNum[1]}" : null,
                        SiteName = siteName,
                        SearchTitle = searchTitle,
                        SearchDate = searchDate,
                        ProviderName = providerName,
                    },
                    Error = new ErrorStructure
                    {
                        Name = e.Message,
                        Text = e.StackTrace,
                    },
                };

                var json = JsonConvert.SerializeObject(AnalitycsData, new JsonSerializerSettings
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

        public struct AnalitycsStructure
        {
            public UserStructure User { get; set; }

            public InfoStructure Info { get; set; }

            public ErrorStructure Error { get; set; }
        }

        public struct UserStructure
        {
            public string UID { get; set; }

            public DateTime DateTime { get; set; }

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
