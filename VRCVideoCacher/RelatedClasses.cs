namespace VRCVideoCacher;

public enum UrlType
{
    YouTube,
    PyPyDance,
    VRDancing,
    Other
}

public class VideoInfo
{
    public string VideoUrl;
    public string VideoId;
    public UrlType UrlType;
}