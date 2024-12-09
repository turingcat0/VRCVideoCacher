using System.Diagnostics;
using Newtonsoft.Json;
using Serilog;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher
{
    public class Updater
    {
        private const string UpdateURL = "https://api.github.com/repos/EllyVR/VRCVideoCacher/releases/latest";
        private static readonly HttpClient _httpClient = new()
        {
            DefaultRequestHeaders = { { "User-Agent", "VRCVideoCacher.Updater" } }
        };
        private static readonly ILogger Log = Program.Logger.ForContext("SourceContext", "Updater");
        private static string path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "VRCVideoCacher.exe");
        private static string bkppath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "VRCVideoCacher.exe.bkp");
        
        public static async Task CheckForUpdates()
        {
            Log.Information("Checking for updates...");
            var response = await _httpClient.GetAsync(UpdateURL);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Failed to check for updates.");
                return;
            }
            var data = await response.Content.ReadAsStringAsync();
            var latestRelease = JsonConvert.DeserializeObject<YtApi>(data);
            Log.Information("Latest release: {Version}", latestRelease.tag_name + " Installed Version: " + Program.Version);
            if (latestRelease.tag_name != Program.Version)
            {
                Log.Information("Update available: {Version}", latestRelease.tag_name);
                //await Update(latestRelease);
                if(ConfigManager.config.AutoUpdate)
                {
                    await UpdateAsync(latestRelease);
                }
                else
                {
                    Log.Information("Auto Update is disabled. Please update manually from the releases page. https://github.com/EllyVR/VRCVideoCacher/releases");
                }
                    
                
            }
            else
            {
                Log.Information("No updates available.");
            }
        }
        
        public static void Cleanup()
        {
            if(File.Exists(bkppath))
                File.Delete(bkppath);
        }
        
        private static async Task UpdateAsync(YtApi release)
        {
            foreach (var asset in release.assets)
            {
                if(asset.name != "VRCVideoCacher.exe")
                    continue;
                
                var stream = await _httpClient.GetStreamAsync(asset.browser_download_url);
                
                File.Move(path, bkppath);
                await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream);
                fileStream.Close();
                
                Log.Information("Updated to version {Version}", release.tag_name);
                try
                {
                    Process p = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true,
                            WorkingDirectory = Path.GetDirectoryName(Environment.ProcessPath)!
                        }
                    };
                    p.Start();
                    Environment.Exit(0);
                }
                catch(Exception ex)
                {
                    Log.Error("Failed to update: {Message}", ex.Message);
                    File.Move("VRCVideoCacher.exe.bkp", "VRCVideoCacher.exe");
                    Console.ReadKey();
                }
                
            }
        }
    }
}