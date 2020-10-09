using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.ScheduledTasks
{
    public class UpdateDatabase : IScheduledTask
    {
        public string Key => Plugin.Instance.Name + "UpdateDatabase";

        public string Name => "Update Database";

        public string Description => "Update database with sites / genres / actors";

        public string Category => Plugin.Instance.Name;

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await Task.Yield();
            progress?.Report(0);

            var data = await HTTP.Request("https://api.github.com/repos/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/contents/data", cancellationToken).ConfigureAwait(false);
            var json = JArray.Parse(data.Content);

            var db = new JObject();
            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.DatabaseHash))
            {
                db = JObject.Parse(Plugin.Instance.Configuration.DatabaseHash);
            }

            for (int i = 0; i < json.Count; i++)
            {
                var file = json[i];

                var url = (string)file["download_url"];
                var fileName = (string)file["name"];
                var sha = (string)file["sha"];

                progress?.Report((double)i / json.Count * 100);

                if (!db.ContainsKey(fileName) || (string)db[fileName] != sha)
                {
                    if (await Database.Download(url, fileName, cancellationToken).ConfigureAwait(false))
                    {
                        if (db.ContainsKey(fileName))
                        {
                            db[fileName] = sha;
                        }
                        else
                        {
                            db.Add(fileName, sha);
                        }
                    }
                }

                Database.Update(fileName);
            }

            Plugin.Instance.Configuration.DatabaseHash = JsonConvert.SerializeObject(db);
            Plugin.Instance.SaveConfiguration();

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup };

            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }
    }
}
