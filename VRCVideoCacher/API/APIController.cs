using System.Diagnostics;
using System.Text;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Serilog;
using Swan.Logging;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher;

public class APIController : WebApiController
{
    public static Serilog.ILogger Log = Program.Logger.ForContext("SourceContext", "APIController");
    [Route(HttpVerbs.Get, "/getvideo")]
    public async Task GetVideo()
    {
        if (ConfigManager.config.RUMode)
        {
            var url = Request.QueryString["url"];
            if (url.Contains("na2.vrdancing.club") || url.Contains("eu2.vrdancing.club"))
            {
                
            }
        }
        if(ConfigManager.config.EnableCache == false)
        {
            await HttpContext.SendStringAsync(ytdlManager.GetURL(Request.QueryString["url"]), "text/plain", Encoding.UTF8);
            return;
        }
        bool isCached = AssetManager.ApiCacheList.Any(x => x.videoUrl == Request.QueryString["url"]);
        try
        {
            var ytdetails = ytdlManager.GetYTJson(Request.QueryString["url"]);
            //Jank to get around null ref please excuse the disgrace to dev
            if (ytdetails != null)
            {
                if (ytdetails)
                {
                    Log.Information("URL Is Live Stream: Bypassing.");
                    await HttpContext.SendStringAsync(ytdlManager.GetURL(Request.QueryString["url"]), "text/plain",
                        Encoding.UTF8);
                    return;
                }
            }
        }
        catch
        {
            Log.Information("Failed to get Live Stream Status: Bypassing.");
            await HttpContext.SendStringAsync(ytdlManager.GetURL(Request.QueryString["url"]), "text/plain",
                Encoding.UTF8);
            return;
        }

        if (ConfigManager.config.BlockedUrls.Contains(Request.QueryString["url"]))
        {
            Console.Beep();
            Console.Beep();
            Log.Information("URL Is Blocked: Bypassing.");
            await HttpContext.SendStringAsync("https://ellyvr.dev/blocked.mp4", "text/plain", Encoding.UTF8);
            return;
        }
        if (Request.QueryString["url"].StartsWith("https://themightygym-europe.ams3.cdn.digitaloceanspaces.com"))
        {
            Log.Information("URL Is Mighty Gym: Bypassing.");
            await HttpContext.SendStringAsync(ytdlManager.GetURL(Request.QueryString["url"]), "text/plain", Encoding.UTF8);
            return;
        }
        if (isCached)
        {
            string localfile = AssetManager.ApiCacheList.First(x => x.videoUrl == Request.QueryString["url"]).FileName;
            string url = $"{ConfigManager.config.ytdlWebServerURL}{localfile}";
            await HttpContext.SendStringAsync(url, "text/plain", Encoding.UTF8);
            return;
        }
        else
        { 
            await HttpContext.SendStringAsync(ytdlManager.GetURL(Request.QueryString["url"]), "text/plain", Encoding.UTF8);
            _ = Task.Run(()=> ytdlManager.DownloadVideo(Request.QueryString["url"]));
            return;
        }
    }
}