using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool ProvideImageSize { get; set; }

        public PluginConfiguration()
        {
            ProvideImageSize = false;
        }
    }
}
