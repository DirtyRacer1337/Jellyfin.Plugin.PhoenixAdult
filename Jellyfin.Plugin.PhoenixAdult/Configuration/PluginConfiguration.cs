using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.DatabaseUpdateURL = "https://api.github.com/repos/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/contents/data";
            this.DatabaseHash = string.Empty;
            this.DisableActors = false;
        }

        public string DatabaseUpdateURL { get; set; }

        public string DatabaseHash { get; set; }

        public bool DisableActors { get; set; }
    }
}
