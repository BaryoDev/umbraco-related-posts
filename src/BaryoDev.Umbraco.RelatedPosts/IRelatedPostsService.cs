using Umbraco.Cms.Core.Models.PublishedContent;

namespace BaryoDev.Umbraco.RelatedPosts;

public interface IRelatedPostsService
{
    /// <summary>Related items for the source, best first. Count falls back to the option.</summary>
    IReadOnlyList<RelatedPost> GetRelated(IPublishedContent source, int? count = null, string? culture = null);

    /// <summary>Same, looked up by key. Empty when the key is not published.</summary>
    IReadOnlyList<RelatedPost> GetRelated(Guid key, int? count = null, string? culture = null);
}
