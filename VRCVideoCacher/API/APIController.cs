using System.Collections;
using System.Diagnostics;
using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Serilog;
using Swan.Logging;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher.API;

public class APIController : WebApiController
{
    private static readonly Serilog.ILogger Log = Program.Logger.ForContext("SourceContext", "APIController");
    
    [Route(HttpVerbs.Get, "/getvideo")]
    public async Task GetVideo()
    {
        var requestUrl = Request.QueryString["url"];
        var avPro = Request.QueryString["avpro"] == "True";
        var videoInfo = await ytdlManager.GetVideoId(requestUrl);
        if (videoInfo == null)
        {
            Log.Information("Failed to get Video Info for URL: {URL}", requestUrl);
            return;
        }
        var fileName = $"{videoInfo.VideoId}.mp4";
        var filePath = Path.Combine(ConfigManager.config.CachedAssetPath, fileName);
        var isCached = File.Exists(filePath);

        var willCache = true;
        if (isCached)
        {
            var url = $"{ConfigManager.config.ytdlWebServerURL}{fileName}";
            Log.Information("Responding with Cached URL: {URL}", url);
            await HttpContext.SendStringAsync(url, "text/plain", Encoding.UTF8);
            return;
        }

        if (string.IsNullOrEmpty(videoInfo.VideoId))
        {
            Log.Information("Failed to get Video ID: Bypassing.");
            willCache = false;
        }

        if (ConfigManager.config.BlockedUrls.Contains(requestUrl))
        {
            Console.Beep();
            Console.Beep();
            Log.Information("URL Is Blocked: Bypassing.");
            await HttpContext.SendStringAsync("https://ellyvr.dev/blocked.mp4", "text/plain", Encoding.UTF8);
            return;
        }
        
        if (requestUrl.StartsWith("https://themightygym-europe.ams3.cdn.digitaloceanspaces.com"))
        {
            Log.Information("URL Is Mighty Gym: Bypassing.");
            willCache = false;
        }
        
        var responseUrl = ytdlManager.GetURL(requestUrl, avPro);
        Log.Information("Responding with URL: {URL}", responseUrl);
        await HttpContext.SendStringAsync(responseUrl, "text/plain", Encoding.UTF8);
        if (willCache)
        {
            ytdlManager.QueueDownload(videoInfo);
        }
    }
}