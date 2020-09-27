using System;
using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public DateTime DatabaseLastUpdate { get; set; }

        public PluginConfiguration()
        {
            DatabaseLastUpdate = DateTime.UtcNow;
        }
    }
}
