using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace BaryoDev.Umbraco.RelatedPosts;

public sealed class RelatedPostsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<RelatedPostsOptions>().BindConfiguration(RelatedPostsOptions.Section);
        builder.Services.AddSingleton<RelatedPostsCache>();
        builder.Services.AddSingleton<IRelatedPostsService, RelatedPostsService>();
        builder.AddNotificationHandler<ContentPublishedNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<ContentUnpublishedNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<ContentMovedToRecycleBinNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<ContentDeletedNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<ContentMovedNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<PublicAccessEntrySavedNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<PublicAccessEntryDeletedNotification, InvalidateOnPublishHandler>();
    }
}
