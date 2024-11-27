using System.Net;

namespace ytdlp;

class Program
{
    public const string URLPath = "url.txt";
    public static void Main(String[] args)
    {
        var baseurl = "http://localhost:9696";
        var url = "";
        string[] arguments = Environment.GetCommandLineArgs();
        foreach (string arg in arguments)
        {
            if (arg.Length >= 4 && string.Equals(arg.Substring(0, 4), "http", StringComparison.OrdinalIgnoreCase))
            {
                url = arg;
            }
        }
        using (WebClient wc = new WebClient())
        {
            if (!String.IsNullOrEmpty(url))
            {
                var inputUrl = Uri.EscapeDataString(url);
                var output = wc.DownloadString($"{baseurl}/api/getvideo?url=" + inputUrl);
                Console.WriteLine(output);
                if (!output.Trim().StartsWith("http"))
                {
                    Environment.ExitCode = 1;
                }
            }
        }
    }
}