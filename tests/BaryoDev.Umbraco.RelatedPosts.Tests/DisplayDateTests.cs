namespace BaryoDev.Umbraco.RelatedPosts.Tests;

public class DisplayDateTests
{
    private static readonly DateTime Updated = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    private static RelatedPost Related(RelatedPostsOptions options, object? publishDate)
    {
        var source = Posts.Post("Source", tags: "a", id: 1);
        var other = Posts.Post("Other", tags: "a", id: 2, publishDate: publishDate);
        return Posts.Service(options, source, other).GetRelated(source).Single();
    }

    [Fact]
    public void Card_date_uses_the_publish_date_property_when_present()
    {
        var related = Related(new RelatedPostsOptions(), new DateTime(2024, 3, 5));

        Assert.Equal(new DateTime(2024, 3, 5), related.DisplayDate);
        Assert.Equal(Updated, related.UpdateDate);
    }

    [Fact]
    public void Card_date_falls_back_to_the_update_date_when_the_property_is_missing()
    {
        Assert.Equal(Updated, Related(new RelatedPostsOptions(), null).DisplayDate);
    }

    [Fact]
    public void Card_date_falls_back_when_the_property_is_empty()
    {
        Assert.Equal(Updated, Related(new RelatedPostsOptions(), default(DateTime)).DisplayDate);
        Assert.Equal(Updated, Related(new RelatedPostsOptions(), "").DisplayDate);
    }

    [Fact]
    public void Card_date_alias_can_be_turned_off()
    {
        var related = Related(new RelatedPostsOptions { DateAlias = "" }, new DateTime(2024, 3, 5));

        Assert.Equal(Updated, related.DisplayDate);
    }

    [Fact]
    public void Card_view_renders_the_display_date()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src"))) dir = dir.Parent;
        var card = File.ReadAllText(Path.Combine(dir!.FullName, "src", "BaryoDev.Umbraco.RelatedPosts", "Views", "Shared", "Components", "RelatedPosts", "_RelatedPostCard.cshtml"));

        Assert.Contains("Model.DisplayDate", card);
        Assert.DoesNotContain("Model.UpdateDate", card);
    }
}
