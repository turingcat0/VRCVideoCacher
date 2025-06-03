using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;
using VRCVideoCacher.Models;

namespace VRCVideoCacher.YTDL;

public class VideoId
{
    private static readonly ILogger Log = Program.Logger.ForContext<VideoId>();
    private static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "VRCVideoCacher" } }
    };
    private static readonly string[] YouTubeHosts = ["youtube.com", "youtu.be", "www.youtube.com"];
    private static readonly Regex YoutubeRegex = new(@"(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|live\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})");


    private static readonly ConcurrentDictionary<string, (string result, bool success, DateTime expiry)> ResolvedUrls = new();

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    private static bool IsYouTubeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return YouTubeHosts.Contains(uri.Host);
        }
        catch
        {
            return false;
        }
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
    
    public static async Task<VideoInfo?> GetVideoId(string url, bool avPro)
    {
        url = url.Trim();
        
        if (url.StartsWith("http://jd.pypy.moe/api/v1/videos/") ||
            url.StartsWith("https://jd.pypy.moe/api/v1/videos/"))
        {
            var req = new HttpRequestMessage(HttpMethod.Head, url);
            var res = await HttpClient.SendAsync(req);
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
                    UrlType = UrlType.PyPyDance,
                    DownloadFormat = DownloadFormat.MP4
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
            var videoId = HashUrl(url);
            return new VideoInfo
            {
                VideoUrl = url,
                VideoId = videoId,
                UrlType = UrlType.VRDancing,
                DownloadFormat = DownloadFormat.MP4
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
                UrlType = UrlType.YouTube,
                DownloadFormat = avPro ? DownloadFormat.Webm : DownloadFormat.MP4,
            };
        }

        var urlHash = HashUrl(url);
        return new VideoInfo
        {
            VideoUrl = url,
            VideoId = urlHash,
            UrlType = UrlType.Other,
            DownloadFormat = DownloadFormat.MP4,
        };
    }

    public static async Task<string> TryGetYouTubeVideoId(string url)
    {
        var additionalArgs = ConfigManager.Config.ytdlAdditionalArgs;
        var cookieArg = string.Empty;
        if (Program.IsCookiesEnabledAndValid())
            cookieArg = "--cookies youtube_cookies.txt";

        var process = new Process
        {
            StartInfo =
            {
                FileName = ConfigManager.Config.ytdlPath,
                Arguments = $"--encoding utf-8 --no-playlist --no-warnings {additionalArgs} {cookieArg} -j \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            }
        };
        process.Start();
        var rawData = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new Exception($"Failed to get video ID: {error.Trim()}");
        if (string.IsNullOrEmpty(rawData))
            throw new Exception("Failed to get video ID");
        var data = JsonConvert.DeserializeObject<dynamic>(rawData);
        if (data is null || data.id is null || data.duration is null)
            throw new Exception("Failed to get video ID");
        if (data.is_live is true)
            throw new Exception("Failed to get video ID: Video is a stream");
        if (data.duration > ConfigManager.Config.CacheYouTubeMaxLength * 60)
            throw new Exception($"Failed to get video ID: Video is longer than configured max length ({data.duration / 60}/{ConfigManager.Config.CacheYouTubeMaxLength})");
        
        return data.id;
    }

    // High bitrate video (1080)
    // https://www.youtube.com/watch?v=DzQwWlbnZvo

    // 4k video
    // https://www.youtube.com/watch?v=i1csLh-0L9E

     public static async Task<Tuple<string, bool>> GetUrl(VideoInfo videoInfo, bool avPro)
    {
        CleanupExpiredCache();

        var cacheKey = $"{videoInfo.VideoUrl}_{avPro}";

        if (ResolvedUrls.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.UtcNow)
        {
            Log.Information("Returning cached URL resolution for: {URL}", videoInfo.VideoUrl);
            return new Tuple<string, bool>(cached.result, cached.success);
        }

        // if url contains "results?" then it's a search
        if (videoInfo.VideoUrl.Contains("results?") && videoInfo.UrlType == UrlType.YouTube)
        {
            const string message = "URL is a search query, cannot get video URL.";
            return new Tuple<string, bool>(message, false);
        }

        var process = new Process
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

        // yt-dlp -f best/bestvideo[height<=?720]+bestaudio --no-playlist --no-warnings --get-url https://youtu.be/GoSo8YOKSAE
        var url = videoInfo.VideoUrl;
        var additionalArgs = ConfigManager.Config.ytdlAdditionalArgs;
        var cookieArg = string.Empty;
        if (Program.IsCookiesEnabledAndValid() && videoInfo.UrlType == UrlType.YouTube)
        {
            var appDirectory = AppContext.BaseDirectory;
            cookieArg = $"--cookies \"{Path.Combine(appDirectory, "youtube_cookies.txt")}\"";
        }



        var languageArg = string.IsNullOrEmpty(ConfigManager.Config.ytdlDubLanguage)
            ? string.Empty
            : $"[language={ConfigManager.Config.ytdlDubLanguage}]/(mp4/best)[height<=?1080][height>=?64][width>=?64]";

        if (avPro)
        {
            process.StartInfo.Arguments = $"--encoding utf-8 -f (mp4/best)[height<=?1080][height>=?64][width>=?64]{languageArg} --impersonate=\"safari\" --extractor-args=\"youtube:player_client=web\" --no-playlist --no-warnings {cookieArg} {additionalArgs} --get-url \"{url}\"";
        }
        else
        {
            process.StartInfo.Arguments = $"--encoding utf-8 -f (mp4/best)[vcodec!=av01][vcodec!=vp9.2][height<=?1080][height>=?64][width>=?64][protocol^=http] --no-playlist --no-warnings {cookieArg} {additionalArgs} --get-url \"{url}\"";
        }

        Log.Information("Started yt-dlp with args: {args}", process.StartInfo.Arguments);
        process.Start();


        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        var checkInterval = 100; // 100ms
        var processTask = process.WaitForExitAsync();

        while (!processTask.IsCompleted)
        {
            if (ResolvedUrls.TryGetValue(cacheKey, out cached) && cached.expiry > DateTime.UtcNow)
            {
                Log.Information("Another request completed while waiting, returning cached result for: {URL}", videoInfo.VideoUrl);

                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        Log.Information("Killed redundant yt-dlp process for: {URL}", videoInfo.VideoUrl);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to kill yt-dlp process: {error}", ex.Message);
                }

                return new Tuple<string, bool>(cached.result, cached.success);
            }

            await Task.WhenAny(processTask, Task.Delay(checkInterval));
        }

        var output = (await outputTask).Trim();
        var error = (await errorTask).Trim();

        if (process.ExitCode != 0)
        {
            if (ResolvedUrls.TryGetValue(cacheKey, out cached) && cached.expiry > DateTime.UtcNow)
            {
                Log.Information("Another request succeeded while we failed, returning cached result for: {URL}", videoInfo.VideoUrl);
                return new Tuple<string, bool>(cached.result, cached.success);
            }

            if (error.Contains("Sign in to confirm you're not a bot"))
                Log.Error("Fix this error by following these instructions: https://github.com/clienthax/VRCVideoCacherBrowserExtension");

            var errorResult = new Tuple<string, bool>(error, false);
            return errorResult;
        }

        if (videoInfo.UrlType == UrlType.YouTube && ConfigManager.Config.ytdlDelay > 0)
        {
            Log.Information("Delaying YouTube URL response for configured {delay} seconds, this can help with video errors, don't ask why", ConfigManager.Config.ytdlDelay);
            await Task.Delay(ConfigManager.Config.ytdlDelay * 1000);
        }

        var result = new Tuple<string, bool>(output, true);
        ResolvedUrls[cacheKey] = (output, true, DateTime.UtcNow.Add(CacheExpiration));

        return result;
    }
    private static void CleanupExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = ResolvedUrls.Where(kvp => kvp.Value.expiry < now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            ResolvedUrls.TryRemove(key, out _);
        }
    }
}