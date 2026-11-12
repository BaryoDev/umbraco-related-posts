using Microsoft.AspNetCore.Mvc;

namespace BaryoDev.Umbraco.RelatedPosts.Tests;

public class ViewTests
{
    // A host site does not register the partial tag helper for a package view, so the
    // <partial> tag would be written to the page as plain text and no card would show.
    [Fact]
    public void Default_view_does_not_rely_on_the_partial_tag_helper()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var view = Path.Combine(dir!.FullName, "src", "BaryoDev.Umbraco.RelatedPosts", "Views", "Shared", "Components", "RelatedPosts", "Default.cshtml");
        var text = File.ReadAllText(view);

        Assert.DoesNotContain("<partial", text);
        Assert.Contains("Html.PartialAsync", text);
    }

    [Fact]
    public void Default_view_writes_the_resolved_stylesheet_and_not_a_fixed_path()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }
        var text = File.ReadAllText(Path.Combine(dir!.FullName, "src", "BaryoDev.Umbraco.RelatedPosts", "Views", "Shared", "Components", "RelatedPosts", "Default.cshtml"));

        Assert.DoesNotContain("/App_Plugins/", text);
        Assert.Contains("Model.Stylesheet", text);
    }

    [Fact]
    public void The_default_stylesheet_path_follows_the_path_base()
    {
        var url = Stub.Make<IUrlHelper>(("Content", a => "/shop" + ((string)a[0]!)[1..]));

        Assert.Equal("/shop/App_Plugins/BaryoDev.RelatedPosts/related-posts.css",
            RelatedPostsViewComponent.ResolveStylesheet(new RelatedPostsOptions().StylesheetPath, url));
    }

    [Fact]
    public void A_stylesheet_path_can_be_absolute_or_empty()
    {
        var url = Stub.Make<IUrlHelper>(("Content", _ => "wrong"));

        Assert.Equal("/css/mine.css", RelatedPostsViewComponent.ResolveStylesheet(" /css/mine.css ", url));
        Assert.Null(RelatedPostsViewComponent.ResolveStylesheet("", url));
        Assert.Null(RelatedPostsViewComponent.ResolveStylesheet(null, url));
    }

    [Fact]
    public void The_stylesheet_is_written_once_per_page()
    {
        var page = new Dictionary<object, object?>();
        var url = Stub.Make<IUrlHelper>(("Content", a => ((string)a[0]!)[1..]));
        var path = new RelatedPostsOptions().StylesheetPath;

        var first = RelatedPostsViewComponent.StylesheetFor(page, 3, path, () => url);
        var second = RelatedPostsViewComponent.StylesheetFor(page, 3, path, () => url);

        Assert.Equal("/App_Plugins/BaryoDev.RelatedPosts/related-posts.css", first);
        Assert.Null(second);
    }

    [Fact]
    public void An_empty_row_or_an_empty_path_writes_no_link_and_does_not_use_up_the_page()
    {
        var page = new Dictionary<object, object?>();
        var url = Stub.Make<IUrlHelper>(("Content", a => ((string)a[0]!)[1..]));

        Assert.Null(RelatedPostsViewComponent.StylesheetFor(page, 0, "~/a.css", () => url));
        Assert.Null(RelatedPostsViewComponent.StylesheetFor(page, 2, "", () => url));
        Assert.Empty(page);
        Assert.Equal("/a.css", RelatedPostsViewComponent.StylesheetFor(page, 2, "~/a.css", () => url));
    }
}
