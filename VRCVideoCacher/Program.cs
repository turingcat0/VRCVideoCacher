using System.Security.Cryptography;
using Serilog;
using Serilog.Core;
using Serilog.Templates;
using Serilog.Templates.Themes;
using VRCVideoCacher.API;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher;

static class Program
{
    public const string ytdlpHash = "tRaaHsCwzCc3t27xEWfOdHGzKhYI+AJCpLfUeWGsCBA=";
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
            GetOurYTDLPHash();
            Environment.Exit(0);
        }
        Console.CancelKeyPress += (_, _) => ConsoleOnCancelKeyPress();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => OnAppQuit();
        
        FileTools.BackupAndReplaceYTDL();
        await ytdlManager.Init();
        WebServer.Init();
        await Task.Delay(-1);
    }

    private static void GetOurYTDLPHash()
    {
        var ytdlStubPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "yt-dlp-stub.exe");
        Console.WriteLine(ComputeBinaryContentHash(File.ReadAllBytes(ytdlStubPath)));
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