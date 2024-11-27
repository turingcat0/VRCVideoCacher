using Swan.Logging;

namespace VRCVideoCacher;

public class WebServerLogger : ILogger
{
    public void Dispose()
    {
        
    }

    public void Log(LogMessageReceivedEventArgs logEvent)
    {
        WebServer.Log.Information($"{logEvent.Message}");
    }

    public LogLevel LogLevel { get; }
}