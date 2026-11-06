using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>Public read-only endpoint for headless sites. Only published content is returned.</summary>
[ApiController]
[Route("related-posts/api/v1")]
public sealed class RelatedPostsApiController : ControllerBase
{
    private readonly IRelatedPostsService _service;
    private readonly IOptionsMonitor<RelatedPostsOptions> _options;
    private readonly ILanguageService _languages;
    private readonly IUmbracoContextAccessor _umbraco;

    public RelatedPostsApiController(IRelatedPostsService service, IOptionsMonitor<RelatedPostsOptions> options, ILanguageService languages, IUmbracoContextAccessor umbraco)
    {
        _service = service;
        _options = options;
        _languages = languages;
        _umbraco = umbraco;
    }

    [HttpGet("{key:guid}")]
    public async Task<IActionResult> Get(Guid key, [FromQuery] int? count, [FromQuery] string? culture)
    {
        if (!_options.CurrentValue.EnableApi || !_service.IsEligible(key)) return NotFound();
        var max = Math.Max(1, _options.CurrentValue.MaxCount);
        var n = count is > 0 ? Math.Min(count.Value, max) : (int?)null;
        string? known = null;
        if (!string.IsNullOrWhiteSpace(culture))
        {
            var codes = (await _languages.GetAllAsync()).Select(l => l.IsoCode);
            known = codes.FirstOrDefault(c => string.Equals(c, culture, StringComparison.OrdinalIgnoreCase));
        }
        var items = _service.GetRelated(key, n, known);
        // Preview or signed-in requests may differ per user, so shared caches must not keep them.
        var personal = (_umbraco.TryGetUmbracoContext(out var ctx) && ctx.InPreviewMode)
            || Request.Headers.ContainsKey("Authorization") || Request.Headers.ContainsKey("Cookie");
        Response.Headers.CacheControl = personal ? "private, no-store" : "public, max-age=60";
        return Ok(new
        {
            items = items.Select(i => new { id = i.Key, name = i.Name, url = i.Url, summary = i.Summary, imageUrl = i.ImageUrl, manual = i.Manual })
        });
    }
}
