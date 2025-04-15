using Serilog;

namespace VRCVideoCacher;

public class FileTools
{
    private static readonly ILogger Log = Program.Logger.ForContext<FileTools>();
    private static readonly string YtdlPath;
    private static readonly string BackupPath;

    static FileTools()
    {
        YtdlPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe";
        BackupPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat\Tools\yt-dlp.exe.bkp";
    }
    
    public static void BackupAndReplaceYtdl()
    {
        if (File.Exists(YtdlPath))
        {
            var hash = Program.ComputeBinaryContentHash(File.ReadAllBytes(YtdlPath));
            if (hash == Program.YtdlpHash)
            {
                Log.Information("YT-DLP is already patched.");
                return;
            }
            if (File.Exists(BackupPath))
            {
                File.SetAttributes(BackupPath, FileAttributes.Normal);
                File.Delete(BackupPath);
            }
            File.Move(YtdlPath, BackupPath);
            Log.Information("Backed up YT-DLP.");
        }
        using var stream = Program.GetYtDlpStub();
        using var fileStream = File.Create(YtdlPath);
        stream.CopyTo(fileStream);
        fileStream.Close();
        var attr = File.GetAttributes(YtdlPath);
        attr |= FileAttributes.ReadOnly;
        File.SetAttributes(YtdlPath, attr);
        Log.Information("Patched YT-DLP.");
    }

    public static void Restore()
    {
        Log.Information("Restoring yt-dlp...");
        if (!File.Exists(BackupPath))
            return;
        
        if (File.Exists(YtdlPath))
        {
            File.SetAttributes(YtdlPath, FileAttributes.Normal);
            File.Delete(YtdlPath);
        }
        File.Move(BackupPath, YtdlPath);
        var attr = File.GetAttributes(YtdlPath);
        attr &= ~FileAttributes.ReadOnly;
        File.SetAttributes(YtdlPath, attr);
        Log.Information("Restored YT-DLP.");
    }
}