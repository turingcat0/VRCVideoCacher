# VRCVideoCacher

VRCVideoCacher is a tool to cache VRChat videos to your local disk.

This is done via a Stubbed yt-dlp executable replaced on start of the application and restored on exit. 
Be sure to exit by hitting Ctrl C in the console window to ensure that vrchats yt-dlp is restored properly.

Below is a sample configuration file. 

```json
{
  "ytdlWebServerURL":"http://localhost:9696",
  "ytdlPath":"Util\\yt-dlp.exe",
  "CachedAssetPath":"D:\\VRCVideoCache"
}
```