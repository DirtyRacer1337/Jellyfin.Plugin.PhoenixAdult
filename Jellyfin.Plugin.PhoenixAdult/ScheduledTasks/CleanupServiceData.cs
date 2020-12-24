using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers;

namespace PhoenixAdult.ScheduledTasks
{
    public class CleanupServiceData : IScheduledTask
    {
        public string Key => Plugin.Instance.Name + "CleanupServiceData";

        public string Name => "Cleanup Service Data";

        public string Description => "Cleanup old service data";

        public string Category => Plugin.Instance.Name;

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
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
                token = Encoding.UTF8.GetString(Helper.ConvertFromBase64String(token.Split('.')[1]));

                if ((int)JObject.Parse(token)["exp"] > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    new_db.Add(site.Key, site.Value);
                }
            }

            Plugin.Instance.Configuration.TokenStorage = JsonConvert.SerializeObject(new_db);
            Plugin.Instance.SaveConfiguration();

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }
    }
}
