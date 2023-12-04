namespace ImagesAdvanced;

public class BrowserOptions
{
    public string? InitialDirectory { get; set; }

    public List<string> Extensions { get; set; } = [];

    public string? RotationCacheFilename { get; set; } = "rotation_cache.txt";

}

