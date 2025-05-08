# VRCVideoCacher

VRCVideoCacher is a tool to cache VRChat videos to your local disk and fix YouTube videos not loading.

This is done via a Stubbed yt-dlp executable replaced on start of the application and restored on exit.

Fixing YouTube bot errors requires a Chrome extension that you can grab from [here](https://chromewebstore.google.com/detail/vrcvideocacher-cookies-ex/kfgelknbegappcajiflgfbjbdpbpokge)

Generate PoToken has been [deprecated](https://github.com/iv-org/youtube-trusted-session-generator?tab=readme-ov-file#tool-is-deprecated)

### Config

| Option                             | Description                                                                                                                                                                                                                                  |
| ---------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ytdlAdditionalArgs                 | Add your own [yt-dlp args](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#usage-and-options), e.g. limit video quality with `-f (mp4/best)[height<=?720][height>=?64][width>=?64]`                                                      |
| ytdlUseCookies                     | Uses the [companion extension](https://github.com/clienthax/VRCVideoCacherBrowserExtension) for cookies, this is used to circumvent YouTubes bot detection.                                                                                  |
| CachedAssetPath                    | Location to store downloaded videos, e.g. store videos on separate drive with `D:\\DownloadedVideos`                                                                                                                                         |
| BlockedUrls                        | List of URLs to never load in VRC                                                                                                                                                                                                            |
| CacheYouTube                       | Download YouTube videos to `CachedAssets` to improve load times next time the video plays.                                                                                                                                                   |
| CacheYouTubeMaxResolution          | Maximum resolution to cache youtube videos in (Larger resolutions will take longer to cache)                                                                                                                                                 |
| CachePyPyDance                     | Download videos that play while you're in [PyPyDance](https://vrchat.com/home/world/wrld_f20326da-f1ac-45fc-a062-609723b097b1)                                                                                                               |
| CacheVRDancing                     | Download videos that play while you're in [VRDancing](https://vrchat.com/home/world/wrld_42377cf1-c54f-45ed-8996-5875b0573a83)                                                                                                               |
| AutoUpdate                         | When a update is available for VRCVideoCacher it will automatically be installed.                                                                                                                                                            |
| PreCacheUrls                       | Download all videos from a JSON list format e.g. `[{"fileName":"video.mp4","url":"https:\/\/example.com\/video.mp4","lastModified":1631653260,"size":124029113},...]` "lastModified" and "size" are optional fields used for file integrity. |
