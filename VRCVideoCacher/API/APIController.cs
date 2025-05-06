using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher.API;

public class ApiController : WebApiController
{
    private static readonly Serilog.ILogger Log = Program.Logger.ForContext<ApiController>();
    
    [Route(HttpVerbs.Post, "/youtube-cookies")]
    public async Task ReceiveYoutubeCookies()
    {
        using var reader = new StreamReader(HttpContext.OpenRequestStream(), Encoding.UTF8);
        var cookies = await reader.ReadToEndAsync();

        await File.WriteAllTextAsync("youtube_cookies.txt", cookies);

        HttpContext.Response.StatusCode = 200;
        await HttpContext.SendStringAsync("Cookies received.", "text/plain", Encoding.UTF8);

        Log.Information("Received Youtube cookies from browser extension.");
        if (!ConfigManager.Config.ytdlUseCookies)
        {
            Log.Warning("Config is NOT set to use cookies from browser extension.");
        }
    }
    
    [Route(HttpVerbs.Get, "/getvideo")]
    public async Task GetVideo()
    {
        var requestUrl = Request.QueryString["url"];
        var avPro = string.Compare(Request.QueryString["avpro"], "true", StringComparison.OrdinalIgnoreCase) == 0;
        if (string.IsNullOrEmpty(requestUrl))
        {
            Log.Error("No URL provided.");
            await HttpContext.SendStringAsync("No URL provided.", "text/plain", Encoding.UTF8);
            return;
        }
        Log.Information("Request URL: {URL}", requestUrl);
        var videoInfo = await VideoId.GetVideoId(requestUrl, avPro);
        if (videoInfo == null)
        {
            Log.Information("Failed to get Video Info for URL: {URL}", requestUrl);
            return;
        }
        var ext = avPro ? "webm" : "mp4";
        var fileName = $"{videoInfo.VideoId}.{ext}";
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
        
        var responseUrl = await VideoId.GetUrl(requestUrl, avPro);
        Log.Information("Responding with URL: {URL}", responseUrl);
        await HttpContext.SendStringAsync(responseUrl, "text/plain", Encoding.UTF8);
        if (willCache)
        {
            VideoDownloader.QueueDownload(videoInfo);
        }
    }
}