using System.Diagnostics;
using System.Text;
using Serilog;

namespace VRCVideoCacher;

public class WinGet
{
    private static readonly ILogger Log = Program.Logger.ForContext<WinGet>();
    private const string WingetExe = "winget.exe";
    private static readonly Dictionary<string, string> _wingetPackages = new()
    {
        { "VP9 Video Extensions", "9n4d0msmp0pt" },
        { "AV1 Video Extension", "9mvzqvxjbq9v" },
        { "Dolby Digital Plus decoder for PC OEMs", "9nvjqjbdkn97" }
    };
    
    public static async Task TryInstallPackages()
    {
        Log.Information("Checking for missing codec packages...");
        if (!IsOurPackagesInstalled())
        {
            Log.Information("Installing missing codec packages...");
            await InstallAllPackages();
        }
    }

    private static bool IsOurPackagesInstalled()
    {
        var installedPackages = GetInstalledPackages();
        if (installedPackages.Count == 0)
        {
            Log.Warning("Failed to get installed winget packages.");
            return true;
        }
        foreach (var package in _wingetPackages.Keys)
        {
            if (!installedPackages.Contains(package))
                return false;
        }

        Log.Information("Codec packages are already installed.");
        return true;
    }

    private static List<string> GetInstalledPackages()
    {
        var installedPackages = new List<string>();
        try
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = WingetExe,
                    Arguments = "list -s msstore --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                if (line == null || string.IsNullOrEmpty(line.Trim()))
                    continue;

                var split = line.Split("  ");
                if (split.Length > 1 && !string.IsNullOrEmpty(split[0]))
                    installedPackages.Add(split[0]);
            }

            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }

        return installedPackages;
    }

    private static async Task InstallAllPackages()
    {
        foreach (var package in _wingetPackages.Values)
        {
            await InstallPackage(package);
        }
    }

    private static async Task InstallPackage(string packageId)
    {
        try
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = WingetExe,
                    Arguments = $"install --id {packageId} -s msstore --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line != null && !string.IsNullOrEmpty(line.Trim()))
                    Log.Debug("{Winget}: " + line, WingetExe);
            }
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                throw new Exception($"Installation failed with exit code {process.ExitCode}. Error: {error}");
            
            var packageName = _wingetPackages.FirstOrDefault(x => x.Value == packageId).Key;
            if (process.ExitCode == 0)
                Log.Information("Successfully installed package: {packageName}", packageName);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
}