using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher.YTDL;

public static class ytdlManager
{
    private static readonly ILogger Log = Program.Logger.ForContext("SourceContext", "YT-DLP");
    private static Thread _downloadThread;
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "VRCVideoCacher" } }
    };
    private static readonly Queue<VideoInfo> _downloadQueue = new();
    private static readonly string _tempDownloadPath;
    private static readonly string _ytdlVersionPath;
    private const string ApiUpdater = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
    
    static ytdlManager()
    {
        _ytdlVersionPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "yt-dlp.version.txt");
        _tempDownloadPath = Path.Combine(ConfigManager.config.CachedAssetPath, "_tempVideo.mp4");
        _downloadThread = new Thread(DownloadThread);
        _downloadThread.Start();
    }

    public static async Task Init()
    {
        Log.Information("Checking for YT-DLP updates...");
        var res = await _httpClient.GetAsync(ApiUpdater);
        var data = await res.Content.ReadAsStringAsync();
        var json = JsonConvert.DeserializeObject<YtApi>(data);
        var currentYtdlVersion = string.Empty;
        if (File.Exists(_ytdlVersionPath))
            currentYtdlVersion = await File.ReadAllTextAsync(_ytdlVersionPath);
        if (string.IsNullOrEmpty(currentYtdlVersion))
            currentYtdlVersion = "Not Installed";
        
        Log.Information($"YT-DLP latest version is {json.tag_name} Current Installed version is {currentYtdlVersion}");
        if (!File.Exists(ConfigManager.config.ytdlPath))
        {
            Log.Information("YT-DLP is not installed. Downloading...");
            await DownloadYtdl(json);
            await File.WriteAllTextAsync(_ytdlVersionPath, json.tag_name);
        }
        else if (currentYtdlVersion != json.tag_name)
        {
            Log.Information("YT-DLP is outdated. Updating...");
            await DownloadYtdl(json);
            await File.WriteAllTextAsync(_ytdlVersionPath, json.tag_name);
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
            
            var stream = await _httpClient.GetStreamAsync(assetVersion.browser_download_url);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigManager.config.ytdlPath));
            await using var fileStream = new FileStream(ConfigManager.config.ytdlPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream);
            Log.Information("Downloaded YT-DLP");
            return;
        }
        throw new Exception("Failed to download YT-DLP");
    }
    
    private static readonly string[] _youTubeHosts = ["youtube.com", "youtu.be", "www.youtube.com"];
    private static bool IsYouTubeUrl(string url)
    {
        try
        {
            var uri = new Uri(url.Trim());
            return _youTubeHosts.Contains(uri.Host);
        }
        catch
        {
            return false;
        }
    }

    public static string GetURL(string url, bool avPro)
    {
        // if url contains "results?" then it's a search
        if (url.Contains("results?"))
        {
            Log.Error("URL is a search query, cannot get video URL.");
            return string.Empty;
        }
        
        var p = new Process
        {
            StartInfo =
            {
                FileName = ConfigManager.config.ytdlPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        
        // yt-dlp -f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings --get-url https://youtu.be/GoSo8YOKSAE
        var additionalArgs = ConfigManager.config.ytdlAdditionalArgs;
        var isYouTube = IsYouTubeUrl(url);
        // TODO: safety check for escaping strings
        if (avPro)
        {
            if (isYouTube)
                p.StartInfo.Arguments =
                    $"-f (mp4/best)[height<=?1080][height>=?64][width>=?64] --impersonate=\"safari\" --extractor-args=\"youtube:player_client=web\" --no-playlist --no-warnings {additionalArgs} --get-url {url}";
            else
                p.StartInfo.Arguments =
                    $"-f (mp4/best)[height<=?1080][height>=?64][width>=?64] --no-playlist --no-warnings {additionalArgs} --get-url {url}";
        }
        else
        {
            p.StartInfo.Arguments =
                $"-f (mp4/best)[vcodec!=av01][vcodec!=vp9.2][height<=?1080][height>=?64][width>=?64][protocol^=http] --no-playlist --no-warnings {additionalArgs} --get-url {url}";
        }
        
        p.Start();
        var output = p.StandardOutput.ReadToEnd();
        if (output.StartsWith("WARNING: ") ||
            output.StartsWith("ERROR: "))
        {
            Log.Error("YouTube Get URL: {output}", output);
            return string.Empty;
        }
        var error = p.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
        {
            Log.Error("YouTube Get URL: {output}", error);
            return string.Empty;
        }

        return output.Trim();
    }
    
    private static string HashUrl(string url)
    {
        return Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(url)))
            .Replace("/", "")
            .Replace("+", "")
            .Replace("=", "");
    }
    
    private static readonly Regex YoutubeRegex = new(@"(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})");
    public static async Task<VideoInfo?> GetVideoId(string url)
    {
        url = url.Trim();
        
        if (url.StartsWith("http://jd.pypy.moe/api/v1/videos/") ||
            url.StartsWith("https://jd.pypy.moe/api/v1/videos/"))
        {
            var req = new HttpRequestMessage(HttpMethod.Head, url);
            var res = await _httpClient.SendAsync(req);
            var videoUrl = res.RequestMessage?.RequestUri?.ToString();
            if (string.IsNullOrEmpty(videoUrl))
            {
                Log.Error("Failed to get video ID from PypyDance URL: {URL}", url);
                return null;
            }
            try
            {
                var uri = new Uri(videoUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                var pypyVideoId = fileName.Split('.')[0];
                return new VideoInfo
                {
                    VideoUrl = videoUrl,
                    VideoId = pypyVideoId,
                    UrlType = UrlType.PyPyDance
                };
            }
            catch
            {
                Log.Error("Failed to get video ID from PypyDance URL: {URL}", url);
                return null;
            }
        }
        
        if (url.StartsWith("https://na2.vrdancing.club") ||
            url.StartsWith("https://eu2.vrdancing.club"))
        {
            return new VideoInfo
            {
                VideoUrl = url,
                VideoId = HashUrl(url),
                UrlType = UrlType.VRDancing
            };
        }
        
        if (IsYouTubeUrl(url))
        {
            var videoId = string.Empty;
            var match = YoutubeRegex.Match(url);
            if (match.Success)
            {
                videoId = match.Groups[1].Value; 
            }
            else if (url.StartsWith("https://www.youtube.com/shorts/") ||
                     url.StartsWith("https://youtube.com/shorts/"))
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var parts = path.Split('/');
                videoId = parts[^1];
            }
            if (string.IsNullOrEmpty(videoId))
            {
                Log.Error("Failed to parse video ID from YouTube URL: {URL}", url);
                return null;
            }
            videoId = videoId.Length > 11 ? videoId.Substring(0, 11) : videoId;
            return new VideoInfo
            {
                VideoUrl = url,
                VideoId = videoId,
                UrlType = UrlType.YouTube
            };
        }
        
        return new VideoInfo
        {
            VideoUrl = url,
            VideoId = HashUrl(url),
            UrlType = UrlType.Other
        };
    }

    private static void DownloadThread()
    {
        while (true)
        {
            if (_downloadQueue.Count == 0)
            {
                Thread.Sleep(100);
                continue;
            }

            var queueItem = _downloadQueue.Peek();
            switch (queueItem.UrlType)
            {
                case UrlType.YouTube:
                    if (ConfigManager.config.CacheYouTube)
                        DownloadYouTubeVideo(queueItem.VideoUrl);
                    break;
                case UrlType.PyPyDance:
                    if (ConfigManager.config.CachePyPyDance)
                        DownloadVideoWithId(queueItem).Wait();
                    break;
                case UrlType.VRDancing:
                    if (ConfigManager.config.CacheVRDancing)
                        DownloadVideoWithId(queueItem).Wait();
                    break;
                case UrlType.Other:
                    // if (ConfigManager.config.CacheOther)
                    //     Log.Error("Download type is not supported yet.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _downloadQueue.Dequeue();
        }
    }
    
    public static void QueueDownload(VideoInfo videoInfo)
    {
        if (_downloadQueue.Any(x => x.VideoUrl == videoInfo.VideoUrl))
        {
            Log.Information("URL is already in the download queue.");
            return;
        }
        _downloadQueue.Enqueue(videoInfo);
    }
    
    private static void DownloadYouTubeVideo(string url)
    {
        var videoId = TryGetYouTubeVideoId(url);
        if (string.IsNullOrEmpty(videoId))
        {
            Log.Information("Not downloading video it's either a stream, invalid or over an hour in length: {URL}", url);
            return;
        }

        if (File.Exists(_tempDownloadPath))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(_tempDownloadPath);
        }
        Log.Information("Downloading YouTube Video: {URL}", url);
        var additionalArgs = ConfigManager.config.ytdlAdditionalArgs;
        var p = new Process
        {
            StartInfo =
            {
                FileName = ConfigManager.config.ytdlPath,
                Arguments =
                    $"-q -o {_tempDownloadPath} -f bv*[height<=1080][vcodec~='^(avc|h264)']+ba[ext=m4a]/bv*[height<=1080][vcodec!=av01][vcodec!=vp9.2][protocol^=http] --no-playlist --remux-video mp4 --no-progress {additionalArgs} -- {videoId}"
                    // $@"-f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings {url} " %(id)s.%(ext)s
            }
        };
        p.Start();
        p.WaitForExit();
        Thread.Sleep(10);
        if (!File.Exists(_tempDownloadPath))
        {
            Log.Error("Failed to download YouTube Video: {URL}", url);
            return;
        }

        var fileName = $"{videoId}.mp4";
        var filePath = Path.Combine(ConfigManager.config.CachedAssetPath, fileName);
        File.Move(_tempDownloadPath, filePath);
        Log.Information("YouTube Video Downloaded: {URL}", $"{ConfigManager.config.ytdlWebServerURL}{fileName}");
    }
    
    private static async Task DownloadVideoWithId(VideoInfo videoInfo)
    {
        if (File.Exists(_tempDownloadPath))
        {
            Log.Error("Temp file already exists, deleting...");
            File.Delete(_tempDownloadPath);
        }
        Log.Information("Downloading Video: {URL}", videoInfo.VideoUrl);
        var url = videoInfo.VideoUrl;
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to download video: {URL}", url);
            return;
        }
        var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(_tempDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        fileStream.Close();
        await Task.Delay(10);
        var fileName = $"{videoInfo.VideoId}.mp4";
        var filePath = Path.Combine(ConfigManager.config.CachedAssetPath, fileName);
        File.Move(_tempDownloadPath, filePath);
        Log.Information("Video Downloaded: {URL}", $"{ConfigManager.config.ytdlWebServerURL}{fileName}");
    }

    private static string? TryGetYouTubeVideoId(string url)
    {
        var p = new Process
        {
            StartInfo =
            {
                FileName = ConfigManager.config.ytdlPath,
                Arguments = $"-j {url}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        p.Start();
        var rawData = p.StandardOutput.ReadToEnd();
        var data = JsonConvert.DeserializeObject<dynamic>(rawData);
        if (data is null ||
            data.id is null ||
            data.is_live is true ||
            data.was_live is true ||
            data.duration is null ||
            data.duration > 3600)
            return null;

        return data.id;
    }
}