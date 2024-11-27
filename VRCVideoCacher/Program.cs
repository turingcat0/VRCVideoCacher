using System.Security.Cryptography;
using Serilog;
using Serilog.Core;
using Serilog.Templates;
using Serilog.Templates.Themes;
using VRCVideoCacher.YTDL;

namespace VRCVideoCacher;

class Program
{
    public const string ytdlphash = "T1bpnlIuM0RIxTiu93g01O7fgPhNPzILASdpOhRuXws=";
    public const string Version = "2024.1.4";
    public static ILogger Logger = Log.ForContext("SourceContext", "Core");
    public static void Main(String[] args)
    {
        Console.Title = $"VRCVideoCacher v{Version}";
        //GetOurYTDLPHash();
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
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        ConfigManager.Load();
        FileTools.BackupAndReplaceYTDL();
        AssetManager.Init();
        ytdlManager.Init();
        WebServer.Init();
        Task.Delay(-1).GetAwaiter().GetResult();
    }

    public static void GetOurYTDLPHash()
    {
        Console.WriteLine(ComputeBinaryContentHash(File.ReadAllBytes("yt-dlp.exe")));
    }
    
    public static string ComputeBinaryContentHash(byte[] base64)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return Convert.ToBase64String(sha256.ComputeHash(base64));
        }
    }
    
    private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        Logger.Information("Resetting yt-dlp config...");
        FileTools.Restore();
        Logger.Information("Exiting...");
        Logger.Information("Press any key to continue...");
        Console.ReadKey();
        Environment.Exit(0);
    }
}

