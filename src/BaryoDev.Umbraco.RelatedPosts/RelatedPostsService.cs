using System.Text.Json;
using BaryoDev.Umbraco.RelatedPosts.Scoring;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace BaryoDev.Umbraco.RelatedPosts;

internal sealed class RelatedPostsService : IRelatedPostsService
{
    private readonly IOptionsMonitor<RelatedPostsOptions> _options;
    private readonly RelatedPostsCache _cache;
    private readonly IPublishedContentCache _content;
    private readonly IPublishedUrlProvider _urls;
    private readonly IPublicAccessService _access;
    private readonly IUmbracoContextAccessor _umbraco;
    private readonly IVariationContextAccessor _variation;
    private readonly IDocumentNavigationQueryService _nav;
    private readonly IPublishedContentStatusFilteringService _status;
    private readonly IDefaultCultureAccessor _defaultCulture;

    public RelatedPostsService(
        IOptionsMonitor<RelatedPostsOptions> options,
        RelatedPostsCache cache,
        IPublishedContentCache content,
        IPublishedUrlProvider urls,
        IPublicAccessService access,
        IUmbracoContextAccessor umbraco,
        IVariationContextAccessor variation,
        IDocumentNavigationQueryService nav,
        IPublishedContentStatusFilteringService status,
        IDefaultCultureAccessor defaultCulture)
    {
        _options = options;
        _cache = cache;
        _content = content;
        _urls = urls;
        _access = access;
        _umbraco = umbraco;
        _variation = variation;
        _nav = nav;
        _status = status;
        _defaultCulture = defaultCulture;
    }

    public IReadOnlyList<RelatedPost> GetRelated(Guid key, int? count = null, string? culture = null)
    {
        var source = _content.GetById(key);
        return source is null ? [] : GetRelated(source, count, culture);
    }

    public bool IsEligible(Guid key)
    {
        var source = _content.GetById(key);
        if (source is null) return false;
        var o = _options.CurrentValue;
        return (o.DocumentTypeAliases.Length == 0
                || o.DocumentTypeAliases.Contains(source.ContentType.Alias, StringComparer.OrdinalIgnoreCase))
               && Allowed(source, o);
    }

    public IReadOnlyList<RelatedPost> GetRelated(IPublishedContent source, int? count = null, string? culture = null)
    {
        var o = _options.CurrentValue;
        var take = Math.Clamp(count ?? o.Count, 0, Math.Max(1, o.MaxCount));
        if (take == 0) return [];

        // Resolve the culture once so the cache key and the reads agree. Without one, Umbraco's
        // publish status check can drop every item, so fall back to the default language.
        var effective = culture ?? _variation.VariationContext?.Culture;
        if (string.IsNullOrEmpty(effective)) effective = _defaultCulture.DefaultCulture;
        var umbCulture = string.IsNullOrEmpty(effective) ? null : effective;

        var ttl = TimeSpan.FromMinutes(Math.Max(1, o.CacheMinutes));
        var preview = _umbraco.TryGetUmbracoContext(out var ctx) && ctx.InPreviewMode;
        var ranked = preview
            ? Rank(source, umbCulture, o)
            : _cache.GetOrCreate($"bdrp:r:{source.Key:N}:{effective}", ttl, () => Rank(source, umbCulture, o));

        return ranked.Take(take).Select(r => ToPost(r.Item, r.Score, r.Manual, o, umbCulture)).ToList();
    }

    private List<Ranked> Rank(IPublishedContent source, string? culture, RelatedPostsOptions o)
    {
        var ttl = TimeSpan.FromMinutes(Math.Max(1, o.CacheMinutes));
        var rootKey = RootKey(source);
        var preview = _umbraco.TryGetUmbracoContext(out var ctx) && ctx.InPreviewMode;
        var candidates = preview
            ? Candidates(rootKey, culture, o)
            : _cache.GetOrCreate($"bdrp:c:{rootKey:N}:{culture}", ttl, () => Candidates(rootKey, culture, o));
        var limit = Math.Max(1, o.MaxCount);

        var sourceTags = Tags(source, o.TagsAlias, culture);
        var sourceCats = Categories(source, o.CategoryAlias, culture);
        var pool = candidates.Where(c => c.Key != source.Key).ToList();

        double[] text = [];
        if (o.EnableTextSimilarity)
            text = TextSimilarity.Score(Tokens(source, o, culture), pool.Select(c => Tokens(c, o, culture)).ToList());

        var scored = new List<(IPublishedContent Item, double Score)>();
        for (var i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            var score = Tags(c, o.TagsAlias, culture).Intersect(sourceTags, StringComparer.OrdinalIgnoreCase).Count() * o.TagWeight;
            if (sourceCats.Count > 0 && Categories(c, o.CategoryAlias, culture).Overlaps(sourceCats)) score += o.CategoryWeight;
            if (o.EnableTextSimilarity) score += text[i] * o.TextWeight;
            if (score > 0 || o.IncludeZeroScore) scored.Add((c, score));
        }

        var result = new List<Ranked>();
        var seen = new HashSet<Guid> { source.Key };

        foreach (var pick in ManualPicks(source, o, culture))
        {
            if (result.Count >= limit) break;
            if (seen.Add(pick.Key)) result.Add(new Ranked(pick, 0, true));
        }

        foreach (var s in scored
                     .OrderByDescending(x => x.Score)
                     .ThenByDescending(x => x.Item.UpdateDate)
                     .ThenBy(x => x.Item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (result.Count >= limit) break;
            if (seen.Add(s.Item.Key)) result.Add(new Ranked(s.Item, Math.Round(s.Score, 3), false));
        }

        return result;
    }

    private sealed record Ranked(IPublishedContent Item, double Score, bool Manual);

    private Guid RootKey(IPublishedContent source) =>
        _nav.TryGetAncestorsKeys(source.Key, out var ancestors) && ancestors.Any() ? ancestors.Last() : source.Key;

    /// <summary>Published, allowed items under the root. Does not exclude the source.</summary>
    private List<IPublishedContent> Candidates(Guid rootKey, string? culture, RelatedPostsOptions o)
    {
        if (!_nav.TryGetDescendantsKeysOrSelfKeys(rootKey, out var keys)) return [];
        var types = new HashSet<string>(o.DocumentTypeAliases, StringComparer.OrdinalIgnoreCase);
        return _status.FilterAvailable(keys, culture)
            .Where(c => (types.Count == 0 || types.Contains(c.ContentType.Alias))
                        && c.IsPublished(culture)
                        && Allowed(c, o))
            .ToList();
    }

    private IEnumerable<IPublishedContent> ManualPicks(IPublishedContent source, RelatedPostsOptions o, string? culture)
    {
        if (string.IsNullOrWhiteSpace(o.ManualAlias)) yield break;
        var value = source.GetProperty(o.ManualAlias)?.GetValue(culture);
        switch (value)
        {
            case IPublishedContent one:
                if (one.IsPublished(culture) && Allowed(one, o)) yield return one;
                break;
            case IEnumerable<IPublishedContent> many:
                foreach (var p in many.Where(p => p.IsPublished(culture) && Allowed(p, o))) yield return p;
                break;
        }
    }

    private bool Allowed(IPublishedContent c, RelatedPostsOptions o) =>
        o.AllowProtected || !_access.IsProtected(c.Path).Success;

    private static List<string> Tags(IPublishedContent c, string alias, string? culture)
    {
        if (string.IsNullOrWhiteSpace(alias)) return [];
        return Strings(c.GetProperty(alias)?.GetValue(culture));
    }

    private static HashSet<string> Categories(IPublishedContent c, string alias, string? culture)
    {
        if (string.IsNullOrWhiteSpace(alias)) return [];
        var value = c.GetProperty(alias)?.GetValue(culture);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        switch (value)
        {
            case IPublishedContent one:
                set.Add(one.Key.ToString("N"));
                break;
            case IEnumerable<IPublishedContent> many:
                foreach (var p in many) set.Add(p.Key.ToString("N"));
                break;
            default:
                foreach (var s in Strings(value)) set.Add(s);
                break;
        }
        return set;
    }

    private static List<string> Strings(object? value) => value switch
    {
        null => [],
        string s => FromString(s),
        IEnumerable<string> many => many.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
        _ => []
    };

    // A JSON array keeps commas inside a tag; anything else is a comma list.
    private static List<string> FromString(string s)
    {
        if (s.AsSpan().TrimStart().StartsWith("["))
        {
            try
            {
                var tags = JsonSerializer.Deserialize<string[]>(s);
                if (tags is not null) return tags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            }
            catch (JsonException)
            {
            }
        }
        return s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private string[] Tokens(IPublishedContent c, RelatedPostsOptions o, string? culture)
    {
        var summary = string.IsNullOrWhiteSpace(o.SummaryAlias) ? null : c.GetProperty(o.SummaryAlias)?.GetValue(culture)?.ToString();
        return TextSimilarity.Tokens($"{c.Name(_variation, culture)} {summary}");
    }

    private RelatedPost ToPost(IPublishedContent c, double score, bool manual, RelatedPostsOptions o, string? culture)
    {
        var summary = string.IsNullOrWhiteSpace(o.SummaryAlias) ? null : c.GetProperty(o.SummaryAlias)?.GetValue(culture)?.ToString();
        string? image = null;
        if (!string.IsNullOrWhiteSpace(o.ImageAlias))
        {
            var media = c.GetProperty(o.ImageAlias)?.GetValue(culture);
            var item = media as IPublishedContent ?? (media as IEnumerable<IPublishedContent>)?.FirstOrDefault();
            image = item?.Url(_urls);
        }
        return new RelatedPost(c.Key, c.Name(_variation, culture) ?? c.Name, c.Url(_urls, culture), summary, image, score, manual, c.UpdateDate);
    }
}
