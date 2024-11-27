using Newtonsoft.Json;

namespace VRCVideoCacher;

public class AssetManager
{
    public static HttpClient client = new HttpClient();
    public static List<ApiVideoCache> ApiCacheList = new List<ApiVideoCache>();
    
    public const string ApiCacheListPath = "Cache.json";

    public static void Init()
    {
        if (!File.Exists(ApiCacheListPath))
        {
            SaveCache();
        }
        else
        {
            ApiCacheList = JsonConvert.DeserializeObject<List<ApiVideoCache>>(File.ReadAllText(ApiCacheListPath));
        }
    }

    public static void SaveCache()
    {
        File.WriteAllText(ApiCacheListPath, JsonConvert.SerializeObject(ApiCacheList, Formatting.Indented));
    }
}

public class ApiVideoCache
{
    public string videoUrl;
    public string FileName;
}