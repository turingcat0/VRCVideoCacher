using System.Reflection;
using System.Security.Cryptography;
using Serilog;
using Serilog.Core;
using Serilog.Templates;
using Serilog.Templates.Themes;
using VRCVideoCacher.API;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher;

internal static class Program
{
    public static string ytdlpHash = string.Empty;
    public const string Version = "2024.11.27";
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

        if (Environment.CommandLine.Contains("--Reset"))
        {
            FileTools.Restore();
            Environment.Exit(0);
        }
        if (Environment.CommandLine.Contains("--Hash"))
        {
            Console.WriteLine(GetOurYTDLPHash());
            Environment.Exit(0);
        }
        Console.CancelKeyPress += (_, _) => ConsoleOnCancelKeyPress();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => OnAppQuit();
        
        ytdlpHash = GetOurYTDLPHash();
        FileTools.BackupAndReplaceYTDL();
        await ytdlManager.Init();
        WebServer.Init();
        await BulkPreCache.DownloadFileList();
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

    public static string GetOurYTDLPHash()
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