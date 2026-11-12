using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Renders the cards. Invoke with <c>Component.InvokeAsync("RelatedPosts", new { content = Model })</c>.</summary>
[ViewComponent(Name = "RelatedPosts")]
public sealed class RelatedPostsViewComponent : ViewComponent
{
    private const string StylesheetShownKey = "BaryoDev.RelatedPosts.StylesheetShown";
    private readonly IRelatedPostsService _service;
    private readonly IOptionsMonitor<RelatedPostsOptions> _options;

    public RelatedPostsViewComponent(IRelatedPostsService service, IOptionsMonitor<RelatedPostsOptions> options)
    {
        _service = service;
        _options = options;
    }

    public IViewComponentResult Invoke(IPublishedContent content, int? count = null, string? heading = null, string? culture = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        var items = _service.GetRelated(content, count, culture);
        var stylesheet = StylesheetFor(HttpContext?.Items, items.Count, _options.CurrentValue.StylesheetPath, () => Url);
        return View(new RelatedPostsViewModel(heading ?? "Read next", items, stylesheet));
    }

    /// <summary>
    /// The link for this render, or null. It is written once per page, by the first component that has items, so
    /// a second invocation on the same page gets none. A fragment cached on its own (output cache, donut caching)
    /// can end up without the link, so such sites set <c>StylesheetPath</c> to empty and include the CSS themselves.
    /// </summary>
    internal static string? StylesheetFor(IDictionary<object, object?>? pageItems, int itemCount, string? path, Func<IUrlHelper?> url)
    {
        if (itemCount == 0 || (pageItems is not null && pageItems.ContainsKey(StylesheetShownKey)))
        {
            return null;
        }
        var href = ResolveStylesheet(path, string.IsNullOrWhiteSpace(path) ? null : url());
        if (href is not null && pageItems is not null)
        {
            pageItems[StylesheetShownKey] = true;
        }
        return href;
    }

    /// <summary>A path starting with ~/ follows the site's path base. Empty gives no link.</summary>
    internal static string? ResolveStylesheet(string? path, IUrlHelper? url)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        var trimmed = path.Trim();
        return trimmed.StartsWith("~/", StringComparison.Ordinal) && url is not null ? url.Content(trimmed) : trimmed;
    }
}

public sealed record RelatedPostsViewModel(string Heading, IReadOnlyList<RelatedPost> Items, string? Stylesheet = null);
