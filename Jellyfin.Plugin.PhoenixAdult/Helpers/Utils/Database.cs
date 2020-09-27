using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PhoenixAdult;

internal static class Database
{
    private const string BaseURL = "https://raw.githubusercontent.com/DirtyRacer1337/Jellyfin.Plugin.PhoenixAdult/master/data/";

    private static readonly string _databasePath = Path.Combine(Plugin.Instance.DataFolderPath, "data");

    private static readonly string[] _databaseFiles = { "SiteList.json", "Actors.json", "Genres.json" };

    public static SiteListStructure SiteList;

    public static ActorsStructure Actors;

    public static GenresStructure Genres;

    public struct SiteListStructure
    {
        public Dictionary<string, string> Abbrieviations { get; set; }
    }

    public struct ActorsStructure
    {
        public Dictionary<string, string[]> ActorsReplace { get; set; }

        public Dictionary<int, string[]> ActorsStudioIndexes { get; set; }

        public Dictionary<int, Dictionary<string, string[]>> ActorsReplaceStudios { get; set; }
    }

    public struct GenresStructure
    {
        public Dictionary<string, string[]> GenresReplace { get; set; }

        public Dictionary<string, string[]> GenresPartialReplace { get; set; }

        public List<string> GenresSkip { get; set; }

        public List<string> GenresPartialSkip { get; set; }
    }

    public static async void Load(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_databasePath))
        {
            Logger.Info($"Creating database directory \"{_databasePath}\"");
            Directory.CreateDirectory(_databasePath);
        }

        foreach (var fileName in _databaseFiles)
        {
            var http = await HTTP.Request(BaseURL + fileName, cancellationToken).ConfigureAwait(false);
            if (http.IsOK)
            {
                Logger.Info($"Database file \"{fileName}\" downloaded successfully");
                File.WriteAllText(Path.Combine(_databasePath, fileName), http.Content);
            }
        }
    }

    public static void Update()
    {
        foreach (var fileName in _databaseFiles)
        {
            var filePath = Path.Combine(_databasePath, fileName);
            if (File.Exists(filePath))
            {
                var data = File.ReadAllText(filePath, Encoding.UTF8);
                switch (fileName)
                {
                    case "SiteList.json":
                        SiteList = JsonConvert.DeserializeObject<SiteListStructure>(data);
                        break;
                    case "Actors.json":
                        Actors = JsonConvert.DeserializeObject<ActorsStructure>(data);
                        break;
                    case "Genres.json":
                        Genres = JsonConvert.DeserializeObject<GenresStructure>(data);
                        break;
                }
            }
        }

        Logger.Info($"Database loading finished");
    }
}
