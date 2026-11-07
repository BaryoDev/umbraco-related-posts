using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Sync;
using Umbraco.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Integration.Testing;

namespace BaryoDev.Umbraco.RelatedPosts.IntegrationTests;

public sealed class MutableOptions : IOptionsMonitor<RelatedPostsOptions>
{
    public RelatedPostsOptions CurrentValue { get; } = new() { IncludeZeroScore = true, Count = 20, MaxCount = 20 };

    public RelatedPostsOptions Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<RelatedPostsOptions, string?> listener) => null;
}

// Answers named members; everything else returns a default.
public class Stub : DispatchProxy
{
    public Dictionary<string, Func<object?[], object?>> On { get; } = new();

    public static T Make<T>(params (string Member, Func<object?[], object?> Answer)[] answers)
        where T : class
    {
        var proxy = Create<T, Stub>();
        foreach (var (member, answer) in answers)
        {
            ((Stub)(object)proxy).On[member] = answer;
        }

        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (On.TryGetValue(targetMethod!.Name, out var answer))
        {
            return answer(args ?? []);
        }

        var type = targetMethod.ReturnType;
        if (type == typeof(string))
        {
            return "/";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return Array.CreateInstance(type.GetGenericArguments()[0], 0);
        }

        return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
    }
}

// Delivers cache refreshers on this server straight away, so a publish updates the published cache and navigation.
public sealed class LocalServerMessenger : ServerMessengerBase
{
    public LocalServerMessenger(IJsonSerializer serializer) : base(false, serializer)
    {
    }

    public override void SendMessages()
    {
    }

    public override void Sync()
    {
    }

    protected override void DeliverRemote(ICacheRefresher refresher, MessageType messageType, IEnumerable<object>? ids = null, string? json = null) =>
        throw new NotSupportedException();
}

public abstract class RelatedPostsFixture : UmbracoIntegrationTest
{
    protected MutableOptions Options { get; private set; } = null!;

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentTreeChangeNotification, ContentTreeChangeDistributedCacheNotificationHandler>();
        builder.Services.AddUnique<IServerMessenger, LocalServerMessenger>();
        new RelatedPostsComposer().Compose(builder);
        Options = new MutableOptions();
        builder.Services.Replace(ServiceDescriptor.Singleton<IOptionsMonitor<RelatedPostsOptions>>(Options));

        // Url building needs a request; the tests only care about which items come back.
        builder.Services.Replace(ServiceDescriptor.Singleton(Stub.Make<IPublishedUrlProvider>(("GetUrl", _ => "/post"))));
    }

    protected IRelatedPostsService Related => GetRequiredService<IRelatedPostsService>();

    protected IContentService Content => GetRequiredService<IContentService>();

    protected void CreatePostType(string alias = "post", ContentVariation variation = ContentVariation.Nothing)
    {
        var type = new ContentType(GetRequiredService<IShortStringHelper>(), Constants.System.Root)
        {
            Alias = alias,
            Name = alias,
            AllowedAsRoot = true,
            Variations = variation,
        };
        var created = GetRequiredService<IContentTypeService>()
            .CreateAsync(type, Constants.Security.SuperUserKey).GetAwaiter().GetResult();
        Assert.That(created.Success, Is.True, "create type: " + created.Result);
    }

    protected IContent Publish(string name, IContent? parent = null, string alias = "post")
    {
        var item = Content.Create(name, parent?.Id ?? Constants.System.Root, alias);
        Content.Save(item);
        var published = Content.Publish(item, ["*"]);
        Assert.That(published.Success, Is.True, "publish " + name + ": " + published.Result);
        return item;
    }
}
