using Newtonsoft.Json;
using Serilog;
using VRCVideoCacher.Models;

namespace VRCVideoCacher.YTDL;

public class YtdlManager
{
    private static readonly ILogger Log = Program.Logger.ForContext<YtdlManager>();
    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "VRCVideoCacher" } }
    };
    private static readonly string YtdlVersionPath;
    private const string ApiUpdater = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
    
    static YtdlManager()
    {
        YtdlVersionPath = Path.Combine(Program.CurrentProcessPath, "yt-dlp.version.txt");
    }

    public static async Task Init()
    {
        Log.Information("Checking for YT-DLP updates...");
        var res = await HttpClient.GetAsync(ApiUpdater);
        var data = await res.Content.ReadAsStringAsync();
        var json = JsonConvert.DeserializeObject<YtApi>(data);
        if (json == null)
        {
            Log.Error("Failed to parse YT-DLP update response.");
            return;
        }
        var currentYtdlVersion = string.Empty;
        if (File.Exists(YtdlVersionPath))
            currentYtdlVersion = await File.ReadAllTextAsync(YtdlVersionPath);
        if (string.IsNullOrEmpty(currentYtdlVersion))
            currentYtdlVersion = "Not Installed";
        
        Log.Information($"YT-DLP latest version is {json.tag_name} Current Installed version is {currentYtdlVersion}");
        if (!File.Exists(ConfigManager.Config.ytdlPath))
        {
            Log.Information("YT-DLP is not installed. Downloading...");
            await DownloadYtdl(json);
            await File.WriteAllTextAsync(YtdlVersionPath, json.tag_name);
        }
        else if (currentYtdlVersion != json.tag_name)
        {
            Log.Information("YT-DLP is outdated. Updating...");
            await DownloadYtdl(json);
            await File.WriteAllTextAsync(YtdlVersionPath, json.tag_name);
        }
        else
        {
            Log.Information("YT-DLP is up to date.");
        }
    }
    
    private static async Task DownloadYtdl(YtApi json)
    {
        foreach (var assetVersion in json.assets)
        {
            if (assetVersion.name != "yt-dlp.exe")
                continue;
            
            var stream = await HttpClient.GetStreamAsync(assetVersion.browser_download_url);
            var path = Path.GetDirectoryName(ConfigManager.Config.ytdlPath);
            if (string.IsNullOrEmpty(path))
                throw new Exception("Failed to get YT-DLP path");
            Directory.CreateDirectory(path);
            await using var fileStream = new FileStream(ConfigManager.Config.ytdlPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream);
            Log.Information("Downloaded YT-DLP");
            return;
        }
        throw new Exception("Failed to download YT-DLP");
    }
}