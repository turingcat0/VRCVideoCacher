# VRCVideoCacher

### What is VRCVideoCacher?

VRCVideoCacher is a tool used to cache VRChat videos to your local disk and fix YouTube videos from failing to load.

### How does it work?

This is done by replacing VRCs yt-dlp.exe with our own stub yt-dlp, this gets replaced on application startup and is restored on exit.

Auto install missing codecs: [VP9](https://apps.microsoft.com/detail/9n4d0msmp0pt) | [AV1](https://apps.microsoft.com/detail/9mvzqvxjbq9v) | [AC-3](https://apps.microsoft.com/detail/9nvjqjbdkn97)

### How to circumvent YouTube bot detection

In order to fix YouTube videos failing to load, you'll need to install our Chrome extension from [here](https://chromewebstore.google.com/detail/vrcvideocacher-cookies-ex/kfgelknbegappcajiflgfbjbdpbpokge) more info [here](https://github.com/clienthax/VRCVideoCacherBrowserExtension), then visit [YouTube.com](https://www.youtube.com) while signed in, at least once while VRCVideoCacher is running, after VRCVideoCacher has obtained your cookies you can safely uninstall the extension.

### Are there any risks involved?

From VRC or EAC? no.

From YouTube/Google? maybe, we strongly recommend you use an alternative Google account if possible.

### Config Options

| Option                    | Description                                                                                                                                                                                                                                  |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ytdlAdditionalArgs        | Add your own [yt-dlp args](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#usage-and-options), e.g. limit video quality with `-f (mp4/best)[height<=?720][height>=?64][width>=?64]`                                                      |
| ytdlUseCookies            | Uses the [companion extension](https://github.com/clienthax/VRCVideoCacherBrowserExtension) for cookies, this is used to circumvent YouTubes bot detection.                                                                                  |
| CachedAssetPath           | Location to store downloaded videos, e.g. store videos on separate drive with `D:\\DownloadedVideos`                                                                                                                                         |
| BlockedUrls               | List of URLs to never load in VRC                                                                                                                                                                                                            |
| CacheYouTube              | Download YouTube videos to `CachedAssets` to improve load times next time the video plays.                                                                                                                                                   |
| CacheYouTubeMaxResolution | Maximum resolution to cache youtube videos in (Larger resolutions will take longer to cache)                                                                                                                                                 |
| CacheYouTubeMaxLength     | Maximum video duration in minutes , e.g. 1hour = 60                                                                                                                                                                                          |
| CachePyPyDance            | Download videos that play while you're in [PyPyDance](https://vrchat.com/home/world/wrld_f20326da-f1ac-45fc-a062-609723b097b1)                                                                                                               |
| CacheVRDancing            | Download videos that play while you're in [VRDancing](https://vrchat.com/home/world/wrld_42377cf1-c54f-45ed-8996-5875b0573a83)                                                                                                               |
| AutoUpdate                | When a update is available for VRCVideoCacher it will automatically be installed.                                                                                                                                                            |
| PreCacheUrls              | Download all videos from a JSON list format e.g. `[{"fileName":"video.mp4","url":"https:\/\/example.com\/video.mp4","lastModified":1631653260,"size":124029113},...]` "lastModified" and "size" are optional fields used for file integrity. |

Generate PoToken has unfortunately been [deprecated](https://github.com/iv-org/youtube-trusted-session-generator?tab=readme-ov-file#tool-is-deprecated)
