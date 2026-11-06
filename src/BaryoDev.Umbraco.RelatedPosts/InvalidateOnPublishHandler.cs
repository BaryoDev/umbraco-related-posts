using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace BaryoDev.Umbraco.RelatedPosts;

internal sealed class InvalidateOnPublishHandler :
    INotificationHandler<ContentPublishedNotification>,
    INotificationHandler<ContentUnpublishedNotification>,
    INotificationHandler<ContentMovedToRecycleBinNotification>,
    INotificationHandler<ContentDeletedNotification>,
    INotificationHandler<ContentMovedNotification>,
    INotificationHandler<PublicAccessEntrySavedNotification>,
    INotificationHandler<PublicAccessEntryDeletedNotification>
{
    private readonly RelatedPostsCache _cache;

    public InvalidateOnPublishHandler(RelatedPostsCache cache) => _cache = cache;

    public void Handle(ContentPublishedNotification notification) => _cache.Clear();
    public void Handle(ContentUnpublishedNotification notification) => _cache.Clear();
    public void Handle(ContentMovedToRecycleBinNotification notification) => _cache.Clear();
    public void Handle(ContentDeletedNotification notification) => _cache.Clear();
    public void Handle(ContentMovedNotification notification) => _cache.Clear();
    public void Handle(PublicAccessEntrySavedNotification notification) => _cache.Clear();
    public void Handle(PublicAccessEntryDeletedNotification notification) => _cache.Clear();
}
