using Newtonsoft.Json;
using Serilog;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace VRCVideoCacher;

public class ConfigManager
{
    public static readonly ConfigModel Config;
    private static readonly ILogger Log = Program.Logger.ForContext<ConfigManager>();
    private static readonly string ConfigFileName = "Config.json";
    
    static ConfigManager()
    {
        Log.Information("Loading config...");
        if (!File.Exists(ConfigFileName))
        {
            Config = new ConfigModel();
            SaveConfig();
        }
        else
        {
            var configFilePath = Path.Combine(Program.CurrentProcessPath, ConfigFileName);
            Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(configFilePath)) ?? new ConfigModel();
            SaveConfig();
        }
        Log.Information("Loaded config.");
    }

    public static void SaveConfig()
    {
        Log.Information("Saving config...");
        File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(Config, Formatting.Indented));
    }
}

// ReSharper disable InconsistentNaming
public class ConfigModel
{
    public string ytdlWebServerURL = "http://localhost:9696/";
    public string ytdlPath = "Utils/yt-dlp.exe";
    public string ytdlAdditionalArgs = string.Empty;
    public string CachedAssetPath = "CachedAssets";
    public string[] BlockedUrls = new[] { "https://na2.vrdancing.club/sampleurl.mp4" };
    public bool CacheYouTube = true;
    public bool CachePyPyDance = true;
    public bool CacheVRDancing = true;

    public bool AutoUpdate = true;
    public string[] PreCacheUrls = [];
}
// ReSharper restore InconsistentNaming