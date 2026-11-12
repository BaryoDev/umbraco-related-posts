using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace BaryoDev.Umbraco.RelatedPosts;

/// <summary>
/// Refresher notifications fire after the published cache updates, and on every server in a
/// load-balanced setup, so the cache is never cleared before fresh data is readable.
/// </summary>
internal sealed class InvalidateOnPublishHandler :
    INotificationHandler<ContentCacheRefresherNotification>,
    INotificationHandler<PublicAccessCacheRefresherNotification>,
    INotificationHandler<MediaCacheRefresherNotification>,
    INotificationHandler<DomainCacheRefresherNotification>,
    INotificationHandler<ContentTypeCacheRefresherNotification>,
    INotificationHandler<LanguageCacheRefresherNotification>
{
    private readonly RelatedPostsCache _cache;

    public InvalidateOnPublishHandler(RelatedPostsCache cache) => _cache = cache;

    public void Handle(ContentCacheRefresherNotification notification) => _cache.Clear();
    public void Handle(PublicAccessCacheRefresherNotification notification) => _cache.Clear();
    public void Handle(MediaCacheRefresherNotification notification) => _cache.Clear();
    public void Handle(DomainCacheRefresherNotification notification) => _cache.Clear();
    public void Handle(ContentTypeCacheRefresherNotification notification) => _cache.Clear();
    public void Handle(LanguageCacheRefresherNotification notification) => _cache.Clear();
}
