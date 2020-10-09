using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.DatabaseHash = string.Empty;
        }

        public string DatabaseHash { get; set; }
    }
}
