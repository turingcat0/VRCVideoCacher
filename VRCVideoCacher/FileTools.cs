using Serilog;

namespace VRCVideoCacher;

public static class FileTools
{
    private static readonly ILogger Log = Program.Logger.ForContext("SourceContext", "Patcher");
    private static readonly string _ytdlPath;
    private static readonly string _backupPath;

    static FileTools()
    {
        _ytdlPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe";
        _backupPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe.bkp";
    }
    
    public static void BackupAndReplaceYTDL()
    {
        if (File.Exists(_ytdlPath))
        {
            var hash = Program.ComputeBinaryContentHash(File.ReadAllBytes(_ytdlPath));
            if (hash == Program.ytdlpHash)
            {
                Log.Information("YT-DLP is already patched.");
                return;
            }
            if (File.Exists(_backupPath))
            {
                File.SetAttributes(_backupPath, FileAttributes.Normal);
                File.Delete(_backupPath);
            }
            File.Move(_ytdlPath, _backupPath);
            Log.Information("Backed up YT-DLP.");
        }
        using var stream = Program.GetYtDlpStub();
        using var fileStream = File.Create(_ytdlPath);
        stream.CopyTo(fileStream);
        fileStream.Close();
        var attr = File.GetAttributes(_ytdlPath);
        attr |= FileAttributes.ReadOnly;
        File.SetAttributes(_ytdlPath, attr);
        Log.Information("Patched YT-DLP.");
    }

    public static void Restore()
    {
        Log.Information("Restoring yt-dlp...");
        if (!File.Exists(_backupPath))
            return;
        
        if (File.Exists(_ytdlPath))
        {
            File.SetAttributes(_ytdlPath, FileAttributes.Normal);
            File.Delete(_ytdlPath);
        }
        File.Move(_backupPath, _ytdlPath);
        Log.Information("Restored YT-DLP.");
    }
}