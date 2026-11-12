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
    /// <summary>Date property shown on the card. Used when present and not empty, else the update date. Empty turns it off.</summary>
    public string DateAlias { get; set; } = "publishDate";
    /// <summary>Content picker whose picks come first. Empty turns it off.</summary>
    public string ManualAlias { get; set; } = "";

    public double TagWeight { get; set; } = 3;
    public double CategoryWeight { get; set; } = 2;
    public double TextWeight { get; set; } = 4;
    public bool EnableTextSimilarity { get; set; }

    public int Count { get; set; } = 3;
    /// <summary>Upper bound for any requested count, including the API.</summary>
    public int MaxCount { get; set; } = 20;
    /// <summary>Off by default: items under public access protection are left out.</summary>
    public bool AllowProtected { get; set; }
    public int CacheMinutes { get; set; } = 60;
    /// <summary>
    /// Most entries the cache keeps. It holds one entry per root and culture, plus one per source post and culture
    /// that was asked for within <c>CacheMinutes</c>. Past this the cache drops about 5 percent of its entries to
    /// make room, so those lists are computed again, and a warning is logged once. Takes effect at start.
    /// </summary>
    public int MaxCacheEntries { get; set; } = 10000;
    /// <summary>Longest summary on a card or in the API, cut at a word edge. 0 means no cut.</summary>
    public int SummaryMaxLength { get; set; } = 300;
    /// <summary>
    /// Stylesheet link the card view writes. A path starting with <c>~/</c> follows the site's path base. Empty writes no link,
    /// for sites that bring their own styles.
    /// </summary>
    public string StylesheetPath { get; set; } = "~/App_Plugins/BaryoDev.RelatedPosts/related-posts.css";
    public bool IncludeZeroScore { get; set; }
    public bool EnableApi { get; set; } = true;
}
