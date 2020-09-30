using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.ScheduledTasks
{
    public class UpdateDatabase : IScheduledTask
    {
        public string Name => "Update Database";

        public string Key => "PhoenixAdultUpdateDatabase";

        public string Description => "Update database with sites / genres / actors";

        public string Category => "PhoenixAdult";

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress?.Report(0);
            for (int i = 0; i < Database.DatabaseFiles.Length; i++)
            {
                var fileName = Database.DatabaseFiles[i];
                progress?.Report((double)i / Database.DatabaseFiles.Length * 100);
                if (await Database.Download(fileName, cancellationToken).ConfigureAwait(false))
                {
                    Database.Update(fileName);
                }
            }
            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup };

            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }
    }
}
