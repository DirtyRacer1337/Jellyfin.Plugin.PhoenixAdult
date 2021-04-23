using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using PhoenixAdult.Configuration;

[assembly: CLSCompliant(false)]

namespace PhoenixAdult
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            this.ConfigurationChanged += this.Configuration.ConfigurationChanged;
        }

        public static Plugin Instance { get; private set; }

        public override string Name => "PhoenixAdult";

        public override Guid Id => Guid.Parse("dc40637f-6ebd-4a34-b8a1-8799629120cf");

        public IEnumerable<PluginPageInfo> GetPages()
            => new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = $"{this.GetType().Namespace}.Configuration.configPage.html",
                },
            };
    }
}
