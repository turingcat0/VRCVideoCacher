using System.Diagnostics;
using Newtonsoft.Json;
using Serilog;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher.YTDL;

public class ytdlManager
{
    public static ILogger Log = Program.Logger.ForContext("SourceContext", "YT-DLP");
    public static void Init()
    {
        Log.Information("Checking for YT-DLP updates...");
        HttpClient c = new HttpClient();
        c.DefaultRequestHeaders.Add("User-Agent", "VRCVideoCacher");
        string apiupdater = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
        var res = c.GetAsync(apiupdater).Result;
        var json = JsonConvert.DeserializeObject<Api>(res.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        string currentVersion = "";
        if(File.Exists("yt-dlp.version.json"))
            currentVersion = File.ReadAllText("yt-dlp.version.json");
        else
            currentVersion = "Not Installed";
        Log.Information("YT-DLP latest version is " + json.tag_name + ". Current Installed version is " + currentVersion);
        if (!File.Exists(ConfigManager.config.ytdlPath))
        {
            if(!Directory.Exists("Utils"))
                Directory.CreateDirectory("Utils");
            File.WriteAllText("yt-dlp.version.json", json.tag_name);
            var bytes = c.GetByteArrayAsync(json.assets[5].browser_download_url).GetAwaiter().GetResult();
            File.WriteAllBytes(ConfigManager.config.ytdlPath, bytes);
            Log.Information("Downloaded YT-DLP");
        }
        else
        {
            if (currentVersion != json.tag_name)
            {
                Log.Information("YT-DLP is outdated. Updating...");
                File.WriteAllText("yt-dlp.version.json", json.tag_name);
                var bytes = c.GetByteArrayAsync(json.assets[5].browser_download_url).GetAwaiter().GetResult();
                File.WriteAllBytes(ConfigManager.config.ytdlPath, bytes);
            }
        }
        
    }

    public static string GetURL(string url)
    {
        //yt-dlp -f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings --get-url https://youtu.be/GoSo8YOKSAE
        Process p = new Process();
        p.StartInfo.FileName = ConfigManager.config.ytdlPath;
        p.StartInfo.Arguments = "-4 -f \"best/bestvideo[height<=?720][ext=mp4][vcodec!=av01][vcodec!=vp9.2]+bestaudio\" --no-playlist --no-warnings --get-url " + url;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        string output = p.StandardOutput.ReadToEnd();
        Log.Information(p.StandardError.ReadToEnd());
        Log.Information($"Retrieved the following URL: {output}");
        return output;
    }
    
    public static void DownloadVideo(string url)
    {
        string id = Guid.NewGuid().ToString();
        Process p = new Process();
        p.StartInfo.FileName = ConfigManager.config.ytdlPath;
        p.StartInfo.Arguments = "-f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings " + url + " -o " + ConfigManager.config.CachedAssetPath + "\\"+ id + ".mp4";
        p.Start();
        AssetManager.ApiCacheList.Add(new ApiVideoCache()
        {
            FileName = $"{id}.mp4",
            videoUrl = url
        });
        AssetManager.SaveCache();
    }

    public static bool GetYTJson(string URL)
    {
        if (!URL.Contains("youtube"))
            return false;

        Process p = new Process();
        p.StartInfo.FileName = ConfigManager.config.ytdlPath;
        p.StartInfo.Arguments = $"-j {URL}";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        //p.WaitForExit();
        string rawdata = p.StandardOutput.ReadToEnd();
        var data = JsonConvert.DeserializeObject<dynamic>(rawdata);
        return data.is_live;
    }
}