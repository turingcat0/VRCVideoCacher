using Swan.Logging;

namespace VRCVideoCacher.API;

public class WebServerLogger : ILogger
{
    public void Dispose()
    {
        
    }

    public void Log(LogMessageReceivedEventArgs logEvent)
    {
        switch (logEvent.MessageType)
        {
            case LogLevel.Error:
                WebServer.Log.Error(logEvent.Message);
                break;
            case LogLevel.Warning:
                WebServer.Log.Warning(logEvent.Message);
                break;
            case LogLevel.Info:
                WebServer.Log.Information(logEvent.Message);
                break;
            default:
                break;
        }
    }

    public LogLevel LogLevel { get; }
}