using System.Diagnostics;
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
    private static readonly string TempDownloadPath;
    
    static VideoDownloader()
    {
        TempDownloadPath = Path.Combine(ConfigManager.Config.CachedAssetPath, "_tempVideo.mp4");
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
                        DownloadYouTubeVideo(queueItem.VideoUrl);
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
    
    private static void DownloadYouTubeVideo(string url)
    {
        var videoId = VideoId.TryGetYouTubeVideoId(url);
        if (string.IsNullOrEmpty(videoId))
        {
            Log.Information("Not downloading video it's either a stream, invalid or over an hour in length: {URL}", url);
            return;
        }

        if (File.Exists(TempDownloadPath))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(TempDownloadPath);
        }
        Log.Information("Downloading YouTube Video: {URL}", url);
        var additionalArgs = ConfigManager.Config.ytdlAdditionalArgs;
        var p = new Process
        {
            StartInfo =
            {
                FileName = ConfigManager.Config.ytdlPath,
                Arguments =
                    $"-q -o {TempDownloadPath} -f bv*[height<=1080][vcodec~='^(avc|h264)']+ba[ext=m4a]/bv*[height<=1080][vcodec!=av01][vcodec!=vp9.2][protocol^=http] --no-playlist --remux-video mp4 --no-progress {additionalArgs} -- {videoId}"
                    // $@"-f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings {url} " %(id)s.%(ext)s
            }
        };
        p.Start();
        p.WaitForExit();
        Thread.Sleep(10);
        if (!File.Exists(TempDownloadPath))
        {
            Log.Error("Failed to download YouTube Video: {URL}", url);
            return;
        }

        var fileName = $"{videoId}.mp4";
        var filePath = Path.Combine(ConfigManager.Config.CachedAssetPath, fileName);
        File.Move(TempDownloadPath, filePath);
        Log.Information("YouTube Video Downloaded: {URL}", $"{ConfigManager.Config.ytdlWebServerURL}{fileName}");
    }
    
    private static async Task DownloadVideoWithId(VideoInfo videoInfo)
    {
        if (File.Exists(TempDownloadPath))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(TempDownloadPath);
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
        await using var fileStream = new FileStream(TempDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        fileStream.Close();
        await Task.Delay(10);
        var fileName = $"{videoInfo.VideoId}.mp4";
        var filePath = Path.Combine(ConfigManager.Config.CachedAssetPath, fileName);
        File.Move(TempDownloadPath, filePath);
        Log.Information("Video Downloaded: {URL}", $"{ConfigManager.Config.ytdlWebServerURL}{fileName}");
    }
}