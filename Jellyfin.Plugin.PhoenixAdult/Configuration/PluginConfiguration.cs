using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public enum JAVActorNamingStyle
    {
        WesternStyle,
        JapaneseStyle,
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.DatabaseUpdateURL = "https://api.github.com/repos/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/contents/data";
            this.DatabaseHash = string.Empty;
            this.DefaultSiteName = string.Empty;
            this.DisableActors = false;

            this.JAVActorNamingStyle = JAVActorNamingStyle.WesternStyle;
        }

        public string DatabaseUpdateURL { get; set; }

        public string DatabaseHash { get; set; }

        public string DefaultSiteName { get; set; }

        public bool DisableActors { get; set; }

        public JAVActorNamingStyle JAVActorNamingStyle { get; set; }
    }
}
