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
        builder.AddNotificationHandler<ContentCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<PublicAccessCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<MediaCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<DomainCacheRefresherNotification, InvalidateOnPublishHandler>();
    }
}
