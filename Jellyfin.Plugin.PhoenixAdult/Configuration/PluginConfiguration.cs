using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string DatabaseHash { get; set; }
        public PluginConfiguration()
        {
            DatabaseHash = string.Empty;
        }
    }
}
