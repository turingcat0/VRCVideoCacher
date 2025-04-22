using Newtonsoft.Json;
using Serilog;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace VRCVideoCacher;

public class ConfigManager
{
    public static readonly ConfigModel Config;
    private static readonly ILogger Log = Program.Logger.ForContext<ConfigManager>();
    private const string ConfigFileName = "Config.json";

    static ConfigManager()
    {
        Log.Information("Loading config...");
        if (!File.Exists(ConfigFileName))
        {
            Config = new ConfigModel();
            FirstRun();
        }
        else
        {
            var configFilePath = Path.Combine(Program.CurrentProcessPath, ConfigFileName);
            Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(configFilePath)) ?? new ConfigModel();
        }
        Log.Information("Loaded config.");
        TrySaveConfig();
    }

    private static void TrySaveConfig()
    {
        var newConfig = JsonConvert.SerializeObject(Config, Formatting.Indented);
        var oldConfig = File.Exists(ConfigFileName) ? File.ReadAllText(ConfigFileName) : string.Empty;
        if (newConfig == oldConfig)
            return;
        
        Log.Information("Config changed, saving...");
        File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(Config, Formatting.Indented));
        Log.Information("Config saved.");
    }
    
    private static void FirstRun()
    {
        Log.Information("It appears this is your first time running VRCVideoCacher. Lets create a basic config file.");
        
        Log.Information("Would you like to cache/download Youtube videos? (Y/n):");
        var input = Console.ReadLine();
        if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            Config.CacheYouTube = true;
        else
            Config.CacheYouTube = false;
        
        Log.Information("Would you like to cache/download VRDancing & PyPyDance videos? (Y/n):");
        input = Console.ReadLine();
        if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
        {
            Config.CacheVRDancing = true;
            Config.CachePyPyDance = true;
        }
        else
        {
            Config.CacheVRDancing = false;
            Config.CachePyPyDance = false;
        }

        Log.Information("Would you like to autogenerate YouTube PoTokens? (This will fix bot errors, requires Chrome to be installed) (Y/n):");
        input = Console.ReadLine();
        if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            Config.ytdlGeneratePoToken = true;
        else
            Config.ytdlGeneratePoToken = false;
        
        Log.Information("Would you like to add VRCVideoCacher to VRCX auto start? (Y/n):");
        input = Console.ReadLine();
        if (string.IsNullOrEmpty(input) || input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            AutoStartShortcut.CreateShortcut();
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