using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Public read-only endpoint for headless sites. Only published content is returned.</summary>
[ApiController]
[Route("related-posts/api/v1")]
public sealed class RelatedPostsApiController : ControllerBase
{
    private readonly IRelatedPostsService _service;
    private readonly IOptionsMonitor<RelatedPostsOptions> _options;

    public RelatedPostsApiController(IRelatedPostsService service, IOptionsMonitor<RelatedPostsOptions> options)
    {
        _service = service;
        _options = options;
    }

    [HttpGet("{key:guid}")]
    public IActionResult Get(Guid key, [FromQuery] int? count, [FromQuery] string? culture)
    {
        if (!_options.CurrentValue.EnableApi) return NotFound();
        var n = count is > 0 ? Math.Min(count.Value, 20) : (int?)null;
        var items = _service.GetRelated(key, n, culture);
        return Ok(new
        {
            items = items.Select(i => new { id = i.Key, name = i.Name, url = i.Url, summary = i.Summary, imageUrl = i.ImageUrl, manual = i.Manual })
        });
    }
}
