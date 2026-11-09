using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace BaryoDev.Umbraco.RelatedPosts.Tests;

internal sealed class FixedOptions(RelatedPostsOptions value) : IOptionsMonitor<RelatedPostsOptions>
{
    public RelatedPostsOptions CurrentValue => value;

    public RelatedPostsOptions Get(string? name) => value;

    public IDisposable? OnChange(Action<RelatedPostsOptions, string?> listener) => null;
}

internal static class Posts
{
    private static readonly DateTime Stamp = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    public static IPublishedContent Post(string name, object? tags = null, string? category = null, int id = 1, object? publishDate = null)
    {
        var key = Guid.NewGuid();
        var type = Stub.Make<IPublishedContentType>(("get_Alias", _ => "post"), ("get_ItemType", _ => PublishedItemType.Content));
        var cultures = new Dictionary<string, PublishedCultureInfo>
        {
            [""] = new PublishedCultureInfo("", name, null, Stamp),
        };
        return Stub.Make<IPublishedContent>(
            ("get_Key", _ => key),
            ("get_ItemType", _ => PublishedItemType.Content),
            ("get_Name", _ => name),
            ("get_ContentType", _ => type),
            ("get_Path", _ => $"-1,{id}"),
            ("get_UpdateDate", _ => Stamp),
            ("get_Cultures", _ => cultures),
            ("IsPublished", _ => true),
            ("GetProperty", a => (string)a[0]! switch
            {
                "tags" when tags is not null => tags == NullValue ? Property(null) : Property(tags),
                "category" when category is not null => Property(category),
                "publishDate" when publishDate is not null => Property(publishDate),
                _ => null,
            }));
    }

    public static readonly object NullValue = new();

    private static IPublishedProperty Property(object? value) =>
        Stub.Make<IPublishedProperty>(("GetValue", _ => value));

    public static RelatedPostsService Service(RelatedPostsOptions options, params IPublishedContent[] all) =>
        Service(options, [], all);

    // Like Umbraco before its publish status cache knows the default language: an empty culture matches nothing.
    public static RelatedPostsService Service(RelatedPostsOptions options, List<string?> cultures, params IPublishedContent[] all)
    {
        var nav = Stub.Make<IDocumentNavigationQueryService>(
            ("TryGetDescendantsKeysOrSelfKeys", a =>
            {
                a[1] = all.Select(c => c.Key).ToArray();
                return true;
            }));
        var status = Stub.Make<IPublishedContentStatusFilteringService>(
            ("FilterAvailable", a =>
            {
                var culture = (string?)a[1];
                cultures.Add(culture);
                if (string.IsNullOrEmpty(culture)) return new List<IPublishedContent>();
                var keys = ((IEnumerable<Guid>)a[0]!).ToHashSet();
                return all.Where(c => keys.Contains(c.Key)).ToList();
            }));
        var urls = Stub.Make<IPublishedUrlProvider>(("GetUrl", _ => "/post"));
        return new RelatedPostsService(
            new FixedOptions(options),
            new RelatedPostsCache(),
            Stub.Make<IPublishedContentCache>(),
            urls,
            Stub.Make<IPublicAccessService>(),
            Stub.Make<IUmbracoContextAccessor>(),
            Stub.Make<IVariationContextAccessor>(),
            nav,
            status,
            Stub.Make<IDefaultCultureAccessor>(("get_DefaultCulture", _ => "en-US")));
    }
}
