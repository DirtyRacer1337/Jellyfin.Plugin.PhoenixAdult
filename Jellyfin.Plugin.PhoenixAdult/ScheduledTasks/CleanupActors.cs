using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using PhoenixAdult.Helpers;

namespace PhoenixAdult.ScheduledTasks
{
    public class CleanupActors : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;

        public CleanupActors(ILibraryManager libraryManager)
        {
            this.libraryManager = libraryManager;
        }

        public string Key => Plugin.Instance.Name + "CleanupActors";

        public string Name => "Cleanup Actors";

        public string Description => "Cleanup actors in library";

        public string Category => Plugin.Instance.Name;

#if __EMBY__
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
#else
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
#endif
        {
            await Task.Yield();
            progress?.Report(0);

            var items = this.libraryManager.GetItemList(new InternalItemsQuery()).Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name)).ToList();

            foreach (var (idx, item) in items.WithIndex())
            {
                List<PersonInfo> peoples;

                progress?.Report((double)idx / items.Count * 100);

#if __EMBY__
                peoples = this.libraryManager.GetItemPeople(item);
#else
                peoples = this.libraryManager.GetPeople(item);
#endif

                if (peoples != null && peoples.Any())
                {
                    var parent = Actors.Cleanup(peoples, item);

                    if (!peoples.Select(o => o.Name).All(parent.Select(o => o.Name).Contains))
                    {
                        Logger.Debug($"Actors cleaned in \"{item.Name}\"");

                        this.libraryManager.UpdatePeople(item, parent);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerWeekly, DayOfWeek = DayOfWeek.Sunday, TimeOfDayTicks = TimeSpan.FromHours(12).Ticks };
        }
    }
}
