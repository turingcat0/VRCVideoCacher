using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher.API;

public class ApiController : WebApiController
{
    private static readonly Serilog.ILogger Log = Program.Logger.ForContext<ApiController>();
    
    [Route(HttpVerbs.Get, "/getvideo")]
    public async Task GetVideo()
    {
        var requestUrl = Request.QueryString["url"];
        var avPro = Request.QueryString["avpro"] == "True";
        if (string.IsNullOrEmpty(requestUrl))
        {
            Log.Information("No URL provided.");
            await HttpContext.SendStringAsync("No URL provided.", "text/plain", Encoding.UTF8);
            return;
        }
        var videoInfo = await VideoId.GetVideoId(requestUrl);
        if (videoInfo == null)
        {
            Log.Information("Failed to get Video Info for URL: {URL}", requestUrl);
            return;
        }
        var fileName = $"{videoInfo.VideoId}.mp4";
        var filePath = Path.Combine(ConfigManager.Config.CachedAssetPath, fileName);
        var isCached = File.Exists(filePath);

        var willCache = true;
        if (isCached)
        {
            var url = $"{ConfigManager.Config.ytdlWebServerURL}{fileName}";
            Log.Information("Responding with Cached URL: {URL}", url);
            await HttpContext.SendStringAsync(url, "text/plain", Encoding.UTF8);
            return;
        }

        if (string.IsNullOrEmpty(videoInfo.VideoId))
        {
            Log.Information("Failed to get Video ID: Bypassing.");
            willCache = false;
        }

        if (ConfigManager.Config.BlockedUrls.Contains(requestUrl))
        {
            Console.Beep();
            Console.Beep();
            Log.Information("URL Is Blocked: Bypassing.");
            await HttpContext.SendStringAsync("https://ellyvr.dev/blocked.mp4", "text/plain", Encoding.UTF8);
            return;
        }
        
        if (requestUrl.StartsWith("https://mightygymcdn.nyc3.cdn.digitaloceanspaces.com"))
        {
            Log.Information("URL Is Mighty Gym: Bypassing.");
            willCache = false;
        }
        
        var responseUrl = VideoId.GetUrl(requestUrl, avPro);
        Log.Information("Responding with URL: {URL}", responseUrl);
        await HttpContext.SendStringAsync(responseUrl, "text/plain", Encoding.UTF8);
        if (willCache)
        {
            VideoDownloader.QueueDownload(videoInfo);
        }
    }
}