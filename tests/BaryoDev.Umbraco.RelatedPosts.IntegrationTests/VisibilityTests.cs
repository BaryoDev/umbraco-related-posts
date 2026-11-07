using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common.Testing;

namespace BaryoDev.Umbraco.RelatedPosts.IntegrationTests;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class VisibilityTests : RelatedPostsFixture
{
    private IContent Unpublished(string name, IContent parent)
    {
        var item = Publish(name, parent);
        var result = Content.Unpublish(item);
        Assert.That(result.Success, Is.True, "unpublish " + name + ": " + result.Result);
        return item;
    }

    [Test]
    public void Unpublished_candidate_is_excluded()
    {
        CreatePostType();
        var root = Publish("Root");
        var source = Publish("Source", root);
        Publish("Live", root);
        Unpublished("Gone", root);

        var names = Related.GetRelated(source.Key).Select(r => r.Name).ToList();

        Assert.That(names, Is.EquivalentTo(new[] { "Root", "Live" }));
    }

    [Test]
    public void Unpublished_section_hides_itself_and_its_children()
    {
        CreatePostType();
        var root = Publish("Root");
        var source = Publish("Source", root);
        var section = Publish("Section", root);
        var child = Publish("Child", section);
        Publish("Live", root);
        Content.Unpublish(section);

        var names = Related.GetRelated(source.Key).Select(r => r.Name).ToList();

        Assert.That(names, Is.EquivalentTo(new[] { "Root", "Live" }));
        Assert.That(names, Does.Not.Contain("Section").And.Not.Contain("Child"));

        // A published child of an unpublished section is not a source either.
        Assert.That(Related.IsEligible(child.Key), Is.False);
        Assert.That(Related.GetRelated(child.Key), Is.Empty);
    }

    [Test]
    public async Task Unpublishing_one_culture_drops_the_item_for_that_culture_only()
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

        var home = Variant("Home", null);
        var source = Variant("Alpha", home);
        var beta = Variant("Beta", home);
        Variant("Gamma", home);
        var dropped = Content.Unpublish(beta, "da-DK");
        Assert.That(dropped.Success, Is.True, "unpublish da-DK: " + dropped.Result);

        var da = Related.GetRelated(source.Key, 10, "da-DK").Select(r => r.Name).ToList();
        var en = Related.GetRelated(source.Key, 10, "en-US").Select(r => r.Name).ToList();

        Assert.That(da, Is.EquivalentTo(new[] { "Home DA", "Gamma DA" }));
        Assert.That(en, Is.EquivalentTo(new[] { "Home EN", "Beta EN", "Gamma EN" }));
    }

    [Test]
    public async Task Tags_are_scored_per_culture()
    {
        var languages = GetRequiredService<ILanguageService>();
        var danish = await languages.CreateAsync(new Language("da-DK", "Danish"), Constants.Security.SuperUserKey);
        Assert.That(danish.Success, Is.True, "create language: " + danish.Status);

        var dataType = await GetRequiredService<IDataTypeService>().GetAsync(Constants.DataTypes.Guids.TextstringGuid);
        Assert.That(dataType, Is.Not.Null, "textstring data type");
        var type = new ContentType(GetRequiredService<IShortStringHelper>(), Constants.System.Root)
        {
            Alias = "tpost",
            Name = "tpost",
            AllowedAsRoot = true,
            Variations = ContentVariation.Culture,
        };
        type.AddPropertyType(new PropertyType(GetRequiredService<IShortStringHelper>(), dataType!)
        {
            Alias = "tags",
            Name = "tags",
            Variations = ContentVariation.Culture,
        });
        var created = await GetRequiredService<IContentTypeService>().CreateAsync(type, Constants.Security.SuperUserKey);
        Assert.That(created.Success, Is.True, "create type: " + created.Result);

        IContent Tagged(string n, IContent? parent, string en, string da)
        {
            var item = Content.Create(n, parent?.Id ?? Constants.System.Root, "tpost");
            item.SetCultureName(n + " EN", "en-US");
            item.SetCultureName(n + " DA", "da-DK");
            item.SetValue("tags", en, culture: "en-US");
            item.SetValue("tags", da, culture: "da-DK");
            Content.Save(item);
            var published = Content.Publish(item, ["en-US", "da-DK"]);
            Assert.That(published.Success, Is.True, "publish " + n + ": " + published.Result);
            return item;
        }

        var home = Tagged("Home", null, "", "");
        var source = Tagged("Alpha", home, "x", "y");
        Tagged("Beta", home, "x", "z");
        Tagged("Gamma", home, "q", "y");
        Options.CurrentValue.IncludeZeroScore = false;

        var en = Related.GetRelated(source.Key, 10, "en-US").Select(r => r.Name).ToList();
        var da = Related.GetRelated(source.Key, 10, "da-DK").Select(r => r.Name).ToList();

        Assert.That(en, Is.EqualTo(new[] { "Beta EN" }));
        Assert.That(da, Is.EqualTo(new[] { "Gamma DA" }));
    }

    [Test]
    public void Manual_picks_come_first_and_are_flagged()
    {
        CreatePostType();
        var root = Publish("Root");
        var sourceContent = Publish("Source", root);
        var pick = Publish("Pick", root);
        Publish("Other", root);
        Options.CurrentValue.ManualAlias = "relatedPicks";
        Options.CurrentValue.EnableTextSimilarity = false;

        var cache = GetRequiredService<IPublishedContentCache>();
        var picked = cache.GetById(pick.Key)!;
        var property = Stub.Make<IPublishedProperty>(("GetValue", _ => new[] { picked }));
        var type = Stub.Make<IPublishedContentType>(("get_Alias", _ => "post"), ("get_ItemType", _ => PublishedItemType.Content));
        var source = Stub.Make<IPublishedContent>(
            ("get_Key", _ => sourceContent.Key),
            ("get_ItemType", _ => PublishedItemType.Content),
            ("get_ContentType", _ => type),
            ("GetProperty", a => (string)a[0]! == "relatedPicks" ? property : null));
        var service = new RelatedPostsService(
            Options,
            GetRequiredService<RelatedPostsCache>(),
            cache,
            GetRequiredService<IPublishedUrlProvider>(),
            GetRequiredService<IPublicAccessService>(),
            GetRequiredService<IUmbracoContextAccessor>(),
            GetRequiredService<IVariationContextAccessor>(),
            GetRequiredService<IDocumentNavigationQueryService>(),
            GetRequiredService<IPublishedContentStatusFilteringService>(),
            GetRequiredService<IDefaultCultureAccessor>());

        var related = service.GetRelated(source, 10);

        Assert.That(related.Select(r => r.Name), Is.EqualTo(new[] { "Pick", "Root", "Other" }).Or.EqualTo(new[] { "Pick", "Other", "Root" }));
        Assert.That(related[0].Manual, Is.True);
        Assert.That(related.Skip(1).Select(r => r.Manual), Is.All.False);
    }

    [Test]
    public void Root_level_source_gets_no_related_posts()
    {
        CreatePostType();
        var top = Publish("Top");
        Publish("Child", top);
        Publish("Sibling");

        Assert.That(Related.GetRelated(top.Key), Is.Empty);
    }

    [Test]
    public void Changing_options_clears_the_cache()
    {
        CreatePostType();
        var root = Publish("Root");
        var source = Publish("Source", root);
        Assert.That(Related.GetRelated(source.Key).Select(r => r.Name), Is.EquivalentTo(new[] { "Root" }));

        // Same cache key, so only a cache clear can show the new filter.
        Options.CurrentValue.DocumentTypeAliases = ["nothing"];
        Assert.That(Related.GetRelated(source.Key).Select(r => r.Name), Is.EquivalentTo(new[] { "Root" }));
        Options.RaiseChanged();

        Assert.That(Related.GetRelated(source.Key), Is.Empty);
    }
}
