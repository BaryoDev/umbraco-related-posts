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
}
