using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.DatabaseHash = string.Empty;
            this.DisableActors = false;
        }

        public string DatabaseHash { get; set; }

        public bool DisableActors { get; set; }
    }
}
