using Serilog;

namespace VRCVideoCacher;

public class FileTools
{
    public static ILogger Log = Program.Logger.ForContext("SourceContext", "Patcher");
    public static void BackupAndReplaceYTDL()
    {
        var ytdlPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe";
        var bkpPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe.bkp";
        var hash = Program.ComputeBinaryContentHash(File.ReadAllBytes(ytdlPath));
        if (hash == Program.ytdlphash)
        {
            Log.Information("YT-DLP is already patched.");
            return;
        }
        if (File.Exists(ytdlPath))
        {
            if (File.Exists(bkpPath))
            {
                File.Delete(bkpPath);
            }
            File.Move(ytdlPath, bkpPath);
            Log.Information("Backed up YT-DLP.");
        }
        File.Copy("yt-dlp.exe", ytdlPath);
        var attr = File.GetAttributes(ytdlPath);
        attr = attr | FileAttributes.ReadOnly;
        File.SetAttributes(ytdlPath, attr);
        Log.Information("Patched YT-DLP.");
    }

    public static void Restore()
    {
        var ytdlPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe";
        var bkpPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe.bkp";
        if (File.Exists(bkpPath))
        {
            File.SetAttributes(ytdlPath, FileAttributes.Normal);
            if (File.Exists(ytdlPath))
            {
                File.Delete(ytdlPath);
            }
            File.Move(bkpPath, ytdlPath);
            
            Log.Information("Restored YT-DLP.");
        }
    }
}