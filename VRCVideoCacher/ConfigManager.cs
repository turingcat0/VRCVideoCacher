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
            FirstRun();
            SaveConfig(true);
        }
        else
        {
            var configFilePath = Path.Combine(Program.CurrentProcessPath, ConfigFileName);
            Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(configFilePath)) ?? new ConfigModel();
            SaveConfig();
        }
        Log.Information("Loaded config.");
    }

    public static void SaveConfig(bool silent = false)
    {
        if (!silent)
            Log.Information("Saving config...");
        File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(Config, Formatting.Indented));
    }
    
    public static void FirstRun()
    {
        Log.Information("It appears this is your first time running VRCVideoCacher. Lets create a basic config file. ");
        Log.Information("Would you like to cache Youtube Videos? (y/n)");
        var input = Console.ReadLine();
        if (input?.ToLower() == "y")
            Config.CacheYouTube = true;
        else
            Config.CacheYouTube = false;
        Log.Information("Would you like to cache PyPyDance Videos? (y/n)");
        input = Console.ReadLine();
        if (input?.ToLower() == "y")
            Config.CachePyPyDance = true;
        else
            Config.CachePyPyDance = false;
        Log.Information("Would you like to cache VRDancing Videos? (y/n)");
        input = Console.ReadLine();
        if (input?.ToLower() == "y")
            Config.CacheVRDancing = true;
        else
            Config.CacheVRDancing = false;
        Log.Information("Would you like to autogenerate a PoToken? (Requires Chrome to be installed) (y/n)");
        input = Console.ReadLine();
        if (input?.ToLower() == "y")
            Config.ytdlGeneratePoToken = true;
        else
            Config.ytdlGeneratePoToken = false;
    }
}

// ReSharper disable InconsistentNaming
public class ConfigModel
{
    public string ytdlWebServerURL = "http://localhost:9696/";
    public string ytdlPath = "Utils/yt-dlp.exe";
    public bool ytdlGeneratePoToken = true;
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