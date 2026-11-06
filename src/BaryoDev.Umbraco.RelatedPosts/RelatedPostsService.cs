using BaryoDev.Umbraco.RelatedPosts.Scoring;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
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
    private readonly INavigationQueryService _nav;
    private readonly IPublishedStatusFilteringService _status;

    public RelatedPostsService(
        IOptionsMonitor<RelatedPostsOptions> options,
        RelatedPostsCache cache,
        IPublishedContentCache content,
        IPublishedUrlProvider urls,
        INavigationQueryService nav,
        IPublishedStatusFilteringService status)
    {
        _options = options;
        _cache = cache;
        _content = content;
        _urls = urls;
        _nav = nav;
        _status = status;
    }

    public IReadOnlyList<RelatedPost> GetRelated(Guid key, int? count = null, string? culture = null)
    {
        var source = _content.GetById(key);
        return source is null ? [] : GetRelated(source, count, culture);
    }

    public IReadOnlyList<RelatedPost> GetRelated(IPublishedContent source, int? count = null, string? culture = null)
    {
        var o = _options.CurrentValue;
        var take = Math.Max(0, count ?? o.Count);
        var cacheKey = $"bdrp:{source.Key:N}:{culture}:{take}";
        return _cache.GetOrCreate(cacheKey, TimeSpan.FromMinutes(Math.Max(1, o.CacheMinutes)),
            () => Compute(source, take, culture, o));
    }

    private IReadOnlyList<RelatedPost> Compute(IPublishedContent source, int take, string? culture, RelatedPostsOptions o)
    {
        if (take == 0) return [];

        var candidates = Candidates(source, culture, o);
        var sourceTags = Tags(source, o.TagsAlias, culture);
        var sourceCats = Categories(source, o.CategoryAlias, culture);

        var sourceTokens = o.EnableTextSimilarity ? Tokens(source, o, culture) : [];
        var docTokens = o.EnableTextSimilarity
            ? candidates.Select(c => Tokens(c, o, culture)).ToList()
            : [];
        var text = o.EnableTextSimilarity ? TextSimilarity.Score(sourceTokens, docTokens) : [];

        var scored = new List<(IPublishedContent Item, double Score)>();
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var score = Tags(c, o.TagsAlias, culture).Intersect(sourceTags, StringComparer.OrdinalIgnoreCase).Count() * o.TagWeight;
            if (sourceCats.Count > 0 && Categories(c, o.CategoryAlias, culture).Overlaps(sourceCats)) score += o.CategoryWeight;
            if (o.EnableTextSimilarity) score += text[i] * o.TextWeight;
            if (score > 0 || o.IncludeZeroScore) scored.Add((c, score));
        }

        var result = new List<RelatedPost>();
        var seen = new HashSet<Guid> { source.Key };

        foreach (var pick in ManualPicks(source, o, culture))
        {
            if (!seen.Add(pick.Key)) continue;
            result.Add(ToPost(pick, 0, true, o, culture));
        }

        foreach (var s in scored
                     .OrderByDescending(x => x.Score)
                     .ThenByDescending(x => x.Item.UpdateDate)
                     .ThenBy(x => x.Item.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!seen.Add(s.Item.Key)) continue;
            result.Add(ToPost(s.Item, Math.Round(s.Score, 3), false, o, culture));
        }

        return result.Take(take).ToList();
    }

    private List<IPublishedContent> Candidates(IPublishedContent source, string? culture, RelatedPostsOptions o)
    {
        var root = source.Root(_nav, _status);
        var types = new HashSet<string>(o.DocumentTypeAliases, StringComparer.OrdinalIgnoreCase);
        return root.DescendantsOrSelf(_nav, _status)
            .Where(c => c.Key != source.Key
                        && (types.Count == 0 || types.Contains(c.ContentType.Alias))
                        && c.IsPublished(culture))
            .ToList();
    }

    private IEnumerable<IPublishedContent> ManualPicks(IPublishedContent source, RelatedPostsOptions o, string? culture)
    {
        if (string.IsNullOrWhiteSpace(o.ManualAlias)) yield break;
        var value = source.GetProperty(o.ManualAlias)?.GetValue(culture);
        switch (value)
        {
            case IPublishedContent one:
                if (one.IsPublished(culture)) yield return one;
                break;
            case IEnumerable<IPublishedContent> many:
                foreach (var p in many.Where(p => p.IsPublished(culture))) yield return p;
                break;
        }
    }

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
        string s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
        IEnumerable<string> many => many.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
        _ => []
    };

    private static string[] Tokens(IPublishedContent c, RelatedPostsOptions o, string? culture)
    {
        var summary = string.IsNullOrWhiteSpace(o.SummaryAlias) ? null : c.GetProperty(o.SummaryAlias)?.GetValue(culture)?.ToString();
        return TextSimilarity.Tokens($"{c.Name(culture)} {summary}");
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
        return new RelatedPost(c.Key, c.Name(culture) ?? c.Name, c.Url(_urls, culture), summary, image, score, manual, c.UpdateDate);
    }
}
