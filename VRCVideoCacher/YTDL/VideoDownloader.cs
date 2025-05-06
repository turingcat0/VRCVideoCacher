using System.Diagnostics;
using System.Net;
using System.Text;
using Serilog;
using VRCVideoCacher.Models;

namespace VRCVideoCacher.YTDL;

public class VideoDownloader
{
    private static readonly ILogger Log = Program.Logger.ForContext<VideoDownloader>();
    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "VRCVideoCacher" } }
    };
    private static readonly Queue<VideoInfo> DownloadQueue = new();
    private static readonly string TempDownloadPathAvPro;
    private static readonly string TempDownloadPathUPlayer;
    
    static VideoDownloader()
    {
        TempDownloadPathAvPro = Path.Combine(ConfigManager.Config.CachedAssetPath, "_tempVideo.webm");
        TempDownloadPathUPlayer = Path.Combine(ConfigManager.Config.CachedAssetPath, "_tempVideo.mp4");
        var downloadThread = new Thread(DownloadThread);
        downloadThread.Start();
    }

    private static void DownloadThread()
    {
        while (true)
        {
            if (DownloadQueue.Count == 0)
            {
                Thread.Sleep(100);
                continue;
            }

            var queueItem = DownloadQueue.Peek();
            switch (queueItem.UrlType)
            {
                case UrlType.YouTube:
                    if (ConfigManager.Config.CacheYouTube)
                        DownloadYouTubeVideo(queueItem.VideoUrl, queueItem.IsAvpro).Wait();
                    break;
                case UrlType.PyPyDance:
                    if (ConfigManager.Config.CachePyPyDance)
                        DownloadVideoWithId(queueItem).Wait();
                    break;
                case UrlType.VRDancing:
                    if (ConfigManager.Config.CacheVRDancing)
                        DownloadVideoWithId(queueItem).Wait();
                    break;
                case UrlType.Other:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            DownloadQueue.Dequeue();
        }
    }
    
    public static void QueueDownload(VideoInfo videoInfo)
    {
        if (DownloadQueue.Any(x => x.VideoUrl == videoInfo.VideoUrl))
        {
            Log.Information("URL is already in the download queue.");
            return;
        }
        DownloadQueue.Enqueue(videoInfo);
    }

    private static async Task DownloadYouTubeVideo(string url, bool isAvpro, bool isRetry = false)
    {
        string? videoId;
        try
        {
            videoId = await VideoId.TryGetYouTubeVideoId(url);
        }
        catch (Exception ex)
        {
            Log.Error("Not downloading YouTube video: {URL} {ex}", url, ex.Message);
            return;
        }

        if (File.Exists(TempDownloadPathAvPro))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(TempDownloadPathAvPro);
        }

        if (File.Exists(TempDownloadPathUPlayer))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(TempDownloadPathUPlayer);
        }

        Log.Information("Downloading YouTube Video: {URL}", url);

        var cookieArg = string.Empty;
        if (ConfigManager.Config.ytdlUseCookies)
            cookieArg = "--cookies youtube_cookies.txt";

        var additionalArgs = ConfigManager.Config.ytdlAdditionalArgs;
        var p = new Process
        {
            StartInfo =
            {
                FileName = ConfigManager.Config.ytdlPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            }
        };

        if (isAvpro)
        {
            p.StartInfo.Arguments = $"--encoding utf-8 -q -o {TempDownloadPathAvPro} -f \"bv*[height<={ConfigManager.Config.CacheYouTubeMaxResolution}][vcodec~='^av01'][ext=mp4][dynamic_range='SDR']+ba[acodec=opus][ext=webm]/bv*[height<={ConfigManager.Config.CacheYouTubeMaxResolution}][vcodec~='vp9'][ext=webm][dynamic_range='SDR']+ba[acodec=opus][ext=webm]\" --no-playlist --extractor-args \"youtube:formats=missing_pot;player_client=web,mweb\" --no-progress {cookieArg} {additionalArgs} -- {videoId}";
            Log.Information("Here be dragons :3 : {args}", p.StartInfo.Arguments);
        }
        else
        {
            // Potato mode.
            p.StartInfo.Arguments = $"--encoding utf-8 -q -o {TempDownloadPathUPlayer} -f \"bv*[height<=1080][vcodec~='^(avc|h264)']+ba[ext=m4a]/bv*[height<=1080][vcodec!=av01][vcodec!=vp9.2][protocol^=http]\" --no-playlist --remux-video mp4 --no-progress {cookieArg} {additionalArgs} -- {videoId}";
            // $@"-f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings {url} " %(id)s.%(ext)s
        }

        p.Start();
        await p.WaitForExitAsync();
        var output = await p.StandardOutput.ReadToEndAsync();
        // TODO: retry cookie fetch
        // if (output.StartsWith("WARNING: ") ||
        //     output.StartsWith("ERROR: "))
        // {
        //     Log.Error("YouTube failed to download: {output}", output);
        //     if (ConfigManager.Config.ytdlGeneratePoToken &&
        //         output.Contains("Sign in to confirm you’re not a bot") &&
        //         !isRetry)
        //     {
        //         await PoTokenGenerator.GeneratePoToken();
        //         Log.Information("Retrying with new POToken...");
        //         await DownloadYouTubeVideo(url, true);
        //         return;
        //     }
        // }
        var error = await p.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(error) && error.Contains("Requested format is not available"))
        {
            Log.Error("Failed to download YouTube Video: {URL} {output}", url, error);
            return;
        }

        Thread.Sleep(10);
        if (isAvpro && !File.Exists(TempDownloadPathAvPro))
        {
            Log.Error("Failed to download YouTube Video: {URL}", url);
            return;
        }

        if(!isAvpro && !File.Exists(TempDownloadPathUPlayer))
        {
            Log.Error("Failed to download YouTube Video: {URL}", url);
            return;
        }

        var ext = isAvpro ? "webm" : "mp4";
        var fileName = $"{videoId}.{ext}";
        var filePath = Path.Combine(ConfigManager.Config.CachedAssetPath, fileName);

        File.Move(isAvpro ? TempDownloadPathAvPro : TempDownloadPathUPlayer, filePath);

        Log.Information("YouTube Video Downloaded: {URL}", $"{ConfigManager.Config.ytdlWebServerURL}{fileName}");
    }
    
    private static async Task DownloadVideoWithId(VideoInfo videoInfo)
    {
        if (videoInfo.IsAvpro && File.Exists(TempDownloadPathAvPro))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(TempDownloadPathAvPro);
        }

        if (!videoInfo.IsAvpro && File.Exists(TempDownloadPathUPlayer))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(TempDownloadPathAvPro);
        }

        Log.Information("Downloading Video: {URL}", videoInfo.VideoUrl);
        var url = videoInfo.VideoUrl;
        var response = await HttpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to download video: {URL}", url);
            return;
        }
        var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(videoInfo.IsAvpro ? TempDownloadPathAvPro : TempDownloadPathUPlayer, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        fileStream.Close();
        await Task.Delay(10);
        var ext = videoInfo.IsAvpro ? "webm" : "mp4";
        var fileName = $"{videoInfo.VideoId}.{ext}";
        var filePath = Path.Combine(ConfigManager.Config.CachedAssetPath, fileName);
        File.Move(videoInfo.IsAvpro ? TempDownloadPathAvPro : TempDownloadPathUPlayer, filePath);
        Log.Information("Video Downloaded: {URL}", $"{ConfigManager.Config.ytdlWebServerURL}{fileName}");
    }
}