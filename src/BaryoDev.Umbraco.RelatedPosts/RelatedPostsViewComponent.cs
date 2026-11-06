using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Renders the cards. Invoke with <c>Component.InvokeAsync("RelatedPosts", new { content = Model })</c>.</summary>
[ViewComponent(Name = "RelatedPosts")]
public sealed class RelatedPostsViewComponent : ViewComponent
{
    private readonly IRelatedPostsService _service;

    public RelatedPostsViewComponent(IRelatedPostsService service) => _service = service;

    public IViewComponentResult Invoke(IPublishedContent content, int? count = null, string? heading = null, string? culture = null)
    {
        var model = new RelatedPostsViewModel(heading ?? "Read next", _service.GetRelated(content, count, culture));
        return View(model);
    }
}

public sealed record RelatedPostsViewModel(string Heading, IReadOnlyList<RelatedPost> Items);
