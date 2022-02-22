using System;
using MediaBrowser.Model.Plugins;

namespace PhoenixAdult.Configuration
{
    public enum JAVActorNamingStyle
    {
        WesternStyle = 0,
        JapaneseStyle = 1,
    }

    public enum GenresSortingStyle
    {
        Alphabetical = 0,
        PositionsLast = 1,
    }

    public enum PreferedActorNameSource
    {
        LocalDatabase = 0,
        NoChange = 1,
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.DatabaseHash = string.Empty;
            this.TokenStorage = string.Empty;

            this.UID = Guid.NewGuid().ToString();
            this.DisableAnalytics = false;
            this.EnableDebug = false;

            this.DisableCaching = false;
            this.DisableSSLCheck = false;

            this.ProxyEnable = false;
            this.ProxyHost = string.Empty;
            this.ProxyPort = 0;
            this.ProxyLogin = string.Empty;
            this.ProxyPassword = string.Empty;

            this.FlareSolverrURL = "http://localhost:8191/";

            this.DefaultSiteName = string.Empty;
            this.UseFilePath = false;

            this.UseMetadataAPI = true;
            this.MetadataAPIToken = string.Empty;

            this.DisableActors = false;
            this.DisableGenres = false;
            this.DisableImageValidation = false;
            this.DisableImageSize = false;
            this.DisableAutoIdentify = false;

            this.JAVActorNamingStyle = JAVActorNamingStyle.WesternStyle;
            this.GenresSortingStyle = GenresSortingStyle.Alphabetical;
            this.PreferedActorNameSource = PreferedActorNameSource.LocalDatabase;
        }

        public string DatabaseHash { get; set; }

        public string TokenStorage { get; set; }

        public string UID { get; set; }

        public bool DisableAnalytics { get; set; }

        public bool EnableDebug { get; set; }

        public bool DisableCaching { get; set; }

        public bool DisableSSLCheck { get; set; }

        public bool ProxyEnable { get; set; }

        public string ProxyHost { get; set; }

        public int ProxyPort { get; set; }

        public string ProxyLogin { get; set; }

        public string ProxyPassword { get; set; }

        public string FlareSolverrURL { get; set; }

        public string DefaultSiteName { get; set; }

        public bool UseFilePath { get; set; }

        public bool UseMetadataAPI { get; set; }

        public string MetadataAPIToken { get; set; }

        public bool DisableActors { get; set; }

        public bool DisableGenres { get; set; }

        public bool DisableImageValidation { get; set; }

        public bool DisableImageSize { get; set; }

        public bool DisableAutoIdentify { get; set; }

        public JAVActorNamingStyle JAVActorNamingStyle { get; set; }

        public GenresSortingStyle GenresSortingStyle { get; set; }

        public PreferedActorNameSource PreferedActorNameSource { get; set; }

#if __EMBY__
#else
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required")]
        public static void ConfigurationChanged(object sender, BasePluginConfiguration e)
        {
            var configuration = Plugin.Instance.Configuration;

            configuration.DefaultSiteName = configuration.DefaultSiteName.Trim();
            configuration.FlareSolverrURL = configuration.FlareSolverrURL.Trim();
            configuration.MetadataAPIToken = configuration.MetadataAPIToken.Trim();

            configuration.ProxyHost = configuration.ProxyHost.Trim();
            configuration.ProxyLogin = configuration.ProxyLogin.Trim();
            configuration.ProxyPassword = configuration.ProxyPassword.Trim();

            Plugin.Instance.SaveConfiguration(configuration);
        }
#endif
    }
}
