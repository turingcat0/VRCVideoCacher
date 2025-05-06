using System.Reflection;
using System.Security.Cryptography;
using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;
using VRCVideoCacher.API;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher;

internal static class Program
{
    public static string YtdlpHash = string.Empty;
    public const string Version = "2025.4.21";
    public static readonly string CurrentProcessPath = Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;
    public static readonly ILogger Logger = Log.ForContext("SourceContext", "Core");
    
    public static async Task Main(string[] args)
    {
        Console.Title = $"VRCVideoCacher v{Version}";
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(new ExpressionTemplate(
                "[{@t:HH:mm:ss} {@l:u3} {Coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1),'<none>')}] {@m}\n{@x}",
                theme: TemplateTheme.Literate))
            .CreateLogger();

        await Updater.CheckForUpdates();
        Updater.Cleanup();
        if (Environment.CommandLine.Contains("--Reset"))
        {
            FileTools.Restore();
            Environment.Exit(0);
        }
        if (Environment.CommandLine.Contains("--Hash"))
        {
            Console.WriteLine(GetOurYtdlpHash());
            Environment.Exit(0);
        }
        Console.CancelKeyPress += (_, _) => ConsoleOnCancelKeyPress();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => OnAppQuit();
        
        YtdlpHash = GetOurYtdlpHash();
        await YtdlManager.TryDownloadYtdlp();
        AutoStartShortcut.TryUpdateShortcutPath();
        WebServer.Init();
        FileTools.BackupAndReplaceYtdl();
        await BulkPreCache.DownloadFileList();
        _ = YtdlManager.TryDownloadFfmpeg();

        // TODO: get cookies at startup if needed
        // if (ConfigManager.Config.ytdlGeneratePoToken)
        // {
        //     try
        //     {
        //         await PoTokenGenerator.TryGeneratePoToken();
        //     }
        //     catch (Exception ex)
        //     {
        //         Logger.Error("Failed to generate PoToken: {Error}", ex.Message);
        //     }
        // }
        
        await Task.Delay(-1);
    }

    public static Stream GetYtDlpStub()
    {
        return GetEmbeddedResource("VRCVideoCacher.yt-dlp-stub.exe");
    }
    
    private static Stream GetEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception($"{resourceName} not found in resources.");

        return stream;
    }

    private static string GetOurYtdlpHash()
    {
        var stream = GetYtDlpStub();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        stream.Dispose();
        return ComputeBinaryContentHash(ms.ToArray());
    }
    
    public static string ComputeBinaryContentHash(byte[] base64)
    {
        return Convert.ToBase64String(SHA256.HashData(base64));
    }

    private static void ConsoleOnCancelKeyPress()
    {
        OnAppQuit();
        Logger.Information("Press any key to continue...");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static void OnAppQuit()
    {
        FileTools.Restore();
        Logger.Information("Exiting...");
    }
}