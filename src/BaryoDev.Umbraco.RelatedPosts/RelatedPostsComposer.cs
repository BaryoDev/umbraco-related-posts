using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace BaryoDev.Umbraco.RelatedPosts;

public sealed class RelatedPostsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<RelatedPostsOptions>().BindConfiguration(RelatedPostsOptions.Section);
        builder.Services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RelatedPostsCache>>();
            var max = sp.GetRequiredService<IOptions<RelatedPostsOptions>>().Value.MaxCacheEntries;
            return new RelatedPostsCache(max, limit => logger.LogWarning(
                "The related posts cache is full at {Limit} entries and is dropping some to make room, so those lists are computed again. Raise BaryoDev:RelatedPosts:MaxCacheEntries if this site has more posts viewed within CacheMinutes.", limit));
        });
        builder.Services.AddSingleton<IRelatedPostsService, RelatedPostsService>();
        builder.AddNotificationHandler<ContentCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<PublicAccessCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<MediaCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<DomainCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<ContentTypeCacheRefresherNotification, InvalidateOnPublishHandler>();
        builder.AddNotificationHandler<LanguageCacheRefresherNotification, InvalidateOnPublishHandler>();
    }
}
