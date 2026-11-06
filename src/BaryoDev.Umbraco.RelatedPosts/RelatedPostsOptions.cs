namespace BaryoDev.Umbraco.RelatedPosts;

public sealed class RelatedPostsOptions
{
    public const string Section = "BaryoDev:RelatedPosts";

    /// <summary>Document types that can be related. Empty means any type.</summary>
    public string[] DocumentTypeAliases { get; set; } = [];
    public string TagsAlias { get; set; } = "tags";
    public string CategoryAlias { get; set; } = "category";
    public string SummaryAlias { get; set; } = "summary";
    public string ImageAlias { get; set; } = "image";
    /// <summary>Content picker whose picks come first. Empty turns it off.</summary>
    public string ManualAlias { get; set; } = "";

    public double TagWeight { get; set; } = 3;
    public double CategoryWeight { get; set; } = 2;
    public double TextWeight { get; set; } = 4;
    public bool EnableTextSimilarity { get; set; }

    public int Count { get; set; } = 3;
    public int CacheMinutes { get; set; } = 60;
    public bool IncludeZeroScore { get; set; }
    public bool EnableApi { get; set; } = true;
}
