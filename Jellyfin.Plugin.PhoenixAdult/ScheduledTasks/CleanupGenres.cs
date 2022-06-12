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
    public class CleanupGenres : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;

        public CleanupGenres(ILibraryManager libraryManager)
        {
            this.libraryManager = libraryManager;
        }

        public string Key => Plugin.Instance.Name + "CleanupGenres";

        public string Name => "Cleanup Genres";

        public string Description => "Cleanup genres in library";

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
                progress?.Report((double)idx / items.Count * 100);

                if (item.Genres != null && item.Genres.Any())
                {
                    var genres = Genres.Cleanup(item.Genres, item.Name);

                    if (!item.Genres.SequenceEqual(genres, StringComparer.Ordinal))
                    {
                        Logger.Debug($"Genres cleaned in \"{item.Name}\"");
                        item.Genres = genres;

#if __EMBY__
                        this.libraryManager.UpdateItem(item, item, ItemUpdateType.MetadataEdit, null);
#else
                        await this.libraryManager.UpdateItemAsync(item, item, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
#endif
                    }
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
