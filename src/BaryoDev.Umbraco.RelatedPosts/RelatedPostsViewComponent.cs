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
        // The link is written once per page, even when the component is invoked more than once.
        string? stylesheet = null;
        var shown = HttpContext?.Items;
        if (items.Count > 0 && (shown is null || !shown.ContainsKey(StylesheetShownKey)))
        {
            stylesheet = ResolveStylesheet(_options.CurrentValue.StylesheetPath, Url);
            if (stylesheet is not null && shown is not null)
            {
                shown[StylesheetShownKey] = true;
            }
        }
        return View(new RelatedPostsViewModel(heading ?? "Read next", items, stylesheet));
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
