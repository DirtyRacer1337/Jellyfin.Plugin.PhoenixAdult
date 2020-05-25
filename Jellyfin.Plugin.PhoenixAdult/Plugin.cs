using System;
using System.Collections.Generic;
using Jellyfin.Plugin.PhoenixAdult.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PhoenixAdult
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => PhoenixAdultProvider.PluginName;
        public override Guid Id => Guid.Parse("dc40637f-6ebd-4a34-b8a1-8799629120cf");
        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        => new[] {
                new PluginPageInfo {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
    }
}
