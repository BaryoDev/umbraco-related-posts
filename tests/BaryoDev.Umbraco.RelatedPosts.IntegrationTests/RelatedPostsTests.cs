using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Tests.Common.Testing;

namespace BaryoDev.Umbraco.RelatedPosts.IntegrationTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class RelatedPostsTests : RelatedPostsFixture
{
    [Test]
    public void Root_is_the_last_ancestor_key()
    {
        CreatePostType();
        // Three levels under each root, so the parent and the root are different nodes.
        var rootA = Publish("A");
        var a1 = Publish("A1", rootA);
        var source = Publish("A2", a1);
        Publish("A3", a1);
        var rootB = Publish("B");
        var b1 = Publish("B1", rootB);
        Publish("B2", b1);

        var names = Related.GetRelated(source.Key).Select(r => r.Name).ToList();

        Assert.That(names, Is.EquivalentTo(new[] { "A", "A1", "A3" }));
        Assert.That(names, Does.Not.Contain("B").And.Not.Contain("B1").And.Not.Contain("B2"));
    }

    [Test]
    public void Protected_candidate_is_excluded()
    {
        CreatePostType();
        var root = Publish("Root");
        var source = Publish("Source", root);
        var open = Publish("Open", root);
        var locked = Publish("Locked", root);
        var access = GetRequiredService<IPublicAccessService>();
        var saved = access.Save(new PublicAccessEntry(
            locked,
            root,
            root,
            [new PublicAccessRule { RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType, RuleValue = "members" }]));
        Assert.That(saved.Success, Is.True, "protect: " + saved.Result);

        Assert.That(Related.IsEligible(locked.Key), Is.False);
        Assert.That(Related.IsEligible(open.Key), Is.True);
        Assert.That(Related.GetRelated(source.Key).Select(r => r.Key), Does.Contain(open.Key).And.Not.Contain(locked.Key));

        Options.CurrentValue.AllowProtected = true;
        GetRequiredService<RelatedPostsCache>().Clear();

        Assert.That(Related.IsEligible(locked.Key), Is.True);
        Assert.That(Related.GetRelated(source.Key).Select(r => r.Key), Does.Contain(locked.Key));
    }

    [Test]
    public async Task Two_cultures_never_mix_names()
    {
        var languages = GetRequiredService<ILanguageService>();
        var danish = await languages.CreateAsync(new Language("da-DK", "Danish"), Constants.Security.SuperUserKey);
        Assert.That(danish.Success, Is.True, "create language: " + danish.Status);
        CreatePostType("vpost", ContentVariation.Culture);

        IContent Variant(string n, IContent? parent)
        {
            var item = Content.Create(n, parent?.Id ?? Constants.System.Root, "vpost");
            item.SetCultureName(n + " EN", "en-US");
            item.SetCultureName(n + " DA", "da-DK");
            Content.Save(item);
            var published = Content.Publish(item, ["en-US", "da-DK"]);
            Assert.That(published.Success, Is.True, "publish " + n + ": " + published.Result);
            return item;
        }

        // Posts sit under a variant section, so the root is the section and not the source itself.
        var home = Variant("Home", null);
        var source = Variant("Alpha", home).Key;
        Variant("Beta", home);
        Variant("Gamma", home);
        var cache = GetRequiredService<RelatedPostsCache>();

        // Empty cache, Danish first.
        var da = Related.GetRelated(source, 10, "da-DK").Select(r => r.Name).ToList();
        Assert.That(da, Is.EquivalentTo(new[] { "Home DA", "Beta DA", "Gamma DA" }));

        // English cached first, then Danish.
        cache.Clear();
        var en = Related.GetRelated(source, 10, "en-US").Select(r => r.Name).ToList();
        Assert.That(en, Is.EquivalentTo(new[] { "Home EN", "Beta EN", "Gamma EN" }));
        var daAfter = Related.GetRelated(source, 10, "da-DK").Select(r => r.Name).ToList();
        Assert.That(daAfter, Is.EquivalentTo(new[] { "Home DA", "Beta DA", "Gamma DA" }));
        Assert.That(Related.GetRelated(source, 10, "en-US").Select(r => r.Name), Is.EquivalentTo(en));
    }

    [Test]
    public void Cache_clears_on_content_cache_refresher()
    {
        var cache = GetRequiredService<RelatedPostsCache>();
        var ttl = TimeSpan.FromMinutes(5);
        Assert.That(cache.GetOrCreate("probe", ttl, () => 1), Is.EqualTo(1));
        Assert.That(cache.GetOrCreate("probe", ttl, () => 2), Is.EqualTo(1));

        GetRequiredService<IEventAggregator>()
            .Publish(new ContentCacheRefresherNotification(Array.Empty<object>(), MessageType.RefreshAll));

        Assert.That(cache.GetOrCreate("probe", ttl, () => 3), Is.EqualTo(3));
    }

    [Test]
    public void Publishing_a_new_post_shows_up_after_a_cached_read()
    {
        CreatePostType();
        var root = Publish("Root");
        var source = Publish("Source", root);
        Assert.That(Related.GetRelated(source.Key).Select(r => r.Name), Is.EquivalentTo(new[] { "Root" }));

        Publish("Fresh", root);

        Assert.That(Related.GetRelated(source.Key).Select(r => r.Name), Is.EquivalentTo(new[] { "Root", "Fresh" }));
    }
}
