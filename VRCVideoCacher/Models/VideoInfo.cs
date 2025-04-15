// ReSharper disable InconsistentNaming
namespace VRCVideoCacher.Models;

public enum UrlType
{
    YouTube,
    PyPyDance,
    VRDancing,
    Other
}

public class VideoInfo
{
    public required string VideoUrl;
    public required string VideoId;
    public required UrlType UrlType;
}