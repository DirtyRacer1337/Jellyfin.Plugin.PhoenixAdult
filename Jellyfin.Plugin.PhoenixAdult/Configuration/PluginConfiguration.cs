using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool IgnoreYearWarning { get; set; }

        public PluginConfiguration()
        {
            IgnoreYearWarning = false;
        }
    }
}
