using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.ScheduledTasks
{
    public class CleanupServiceData : IScheduledTask
    {
        public string Key => Plugin.Instance.Name + "CleanupServiceData";

        public string Name => "Cleanup Service Data";

        public string Description => "Cleanup old service data";

        public string Category => Plugin.Instance.Name;

#if __EMBY__
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
#else
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
#endif
        {
            await Task.Yield();
            progress?.Report(0);

            var db = new JObject();
            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.TokenStorage))
            {
                db = JObject.Parse(Plugin.Instance.Configuration.TokenStorage);
            }

            var new_db = new JObject();
            foreach (var site in db)
            {
                var token = (string)site.Value;
                var timestamp = 0;

                if (token.Contains('.'))
                {
                    token = Encoding.UTF8.GetString(Helper.ConvertFromBase64String(token.Split('.')[1]) ?? Array.Empty<byte>());
                    timestamp = (int)JObject.Parse(token)["exp"];
                }
                else
                {
                    token = Encoding.UTF8.GetString(Helper.ConvertFromBase64String(token) ?? Array.Empty<byte>());
                    if (token.Contains("validUntil") && int.TryParse(token.Split("validUntil=")[1].Split("&")[0], out timestamp))
                    {
                    }
                }

                if (timestamp > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    new_db.Add(site.Key, site.Value);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            Plugin.Instance.Configuration.TokenStorage = JsonConvert.SerializeObject(new_db);
            Plugin.Instance.SaveConfiguration();

            if (Directory.Exists(Analytics.LogsPath))
            {
                foreach (var file in Directory.GetFiles(Analytics.LogsPath, "*.json.gz"))
                {
                    if (Math.Abs((DateTime.Now - File.GetCreationTime(file)).TotalDays) > 3)
                    {
                        File.Delete(file);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }
    }
}
