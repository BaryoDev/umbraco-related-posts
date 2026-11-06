using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace BaryoDev.Umbraco.RelatedPosts;

internal sealed class InvalidateOnPublishHandler :
    INotificationHandler<ContentPublishedNotification>,
    INotificationHandler<ContentUnpublishedNotification>
{
    private readonly RelatedPostsCache _cache;

    public InvalidateOnPublishHandler(RelatedPostsCache cache) => _cache = cache;

    public void Handle(ContentPublishedNotification notification) => _cache.Clear();
    public void Handle(ContentUnpublishedNotification notification) => _cache.Clear();
}
