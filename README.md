# VRCVideoCacher

VRCVideoCacher is a tool to cache VRChat videos to your local disk and fix YouTube videos not loading.

This is done via a Stubbed yt-dlp executable replaced on start of the application and restored on exit.

### Config

| Option              | Description                                                                                                                                                                                                                                  |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ytdlGeneratePoToken | Install and run [youtube-trusted-session-generator](https://github.com/iv-org/youtube-trusted-session-generator), it will briefly open a Chrome window to generate a new token, this is used to circumvent YouTube's bot detection.          |
| ytdlAdditionalArgs  | Add your own [yt-dlp args](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#usage-and-options), e.g. limit video quality with `-f (mp4/best)[height<=?720][height>=?64][width>=?64]`                                                      |
| CachedAssetPath     | Location to store downloaded videos, e.g. store videos on separate drive with `D:\\DownloadedVideos`                                                                                                                                         |
| BlockedUrls         | List of URLs to never load in VRC                                                                                                                                                                                                            |
| CacheYouTube        | Download YouTube videos to `CachedAssets` to improve load times next time the video plays.                                                                                                                                                   |
| CachePyPyDance      | Download videos that play while you're in [PyPyDance](https://vrchat.com/home/world/wrld_f20326da-f1ac-45fc-a062-609723b097b1)                                                                                                               |
| CacheVRDancing      | Download videos that play while you're in [VRDancing](https://vrchat.com/home/world/wrld_42377cf1-c54f-45ed-8996-5875b0573a83)                                                                                                               |
| AutoUpdate          | When a update is available for VRCVideoCacher it will automatically be installed.                                                                                                                                                            |
| PreCacheUrls        | Download all videos from a JSON list format e.g. `[{"fileName":"video.mp4","url":"https:\/\/example.com\/video.mp4","lastModified":1631653260,"size":124029113},...]` "lastModified" and "size" are optional fields used for file integrity. |
