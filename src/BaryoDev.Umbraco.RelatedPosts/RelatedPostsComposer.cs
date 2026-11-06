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
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<RelatedPostsCache>();
        builder.Services.AddSingleton<IRelatedPostsService, RelatedPostsService>();
        builder.AddNotificationHandler<ContentPublishedNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<ContentUnpublishedNotification, InvalidateOnPublishHandler>();
    }
}
