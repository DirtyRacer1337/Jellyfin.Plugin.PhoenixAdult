using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace PhoenixAdult.ScheduledTasks
{
    public class UpdateDatabase : IScheduledTask
    {
        public string Name => "Update Database";

        public string Key => "PhoenixAdultUpdateDatabase";

        public string Description => "Update database with sites abbreviations / genres / actors";

        public string Category => "PhoenixAdult";

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress.Report(0);
            Database.Load(cancellationToken);
            progress.Report(50);
            Database.Update();
            progress.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerStartup };

            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks };
        }
    }
}
