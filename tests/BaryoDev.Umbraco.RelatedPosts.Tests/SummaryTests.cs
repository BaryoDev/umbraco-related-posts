namespace BaryoDev.Umbraco.RelatedPosts.Tests;

public class SummaryTests
{
    [Fact]
    public void A_long_summary_is_cut_at_a_word_edge_to_the_cap()
    {
        var text = string.Join(' ', Enumerable.Repeat("wordy", 2000));

        var cut = SummaryText.Cut(text, 300)!;

        Assert.True(cut.Length <= 303);
        Assert.EndsWith("wordy...", cut);
    }

    [Fact]
    public void A_short_summary_and_a_zero_cap_are_left_alone()
    {
        Assert.Equal("Short.", SummaryText.Cut("Short.", 300));
        Assert.Equal(new string('a', 5000), SummaryText.Cut(new string('a', 5000), 0));
        Assert.Null(SummaryText.Cut(null, 300));
    }

    [Fact]
    public void The_service_caps_the_summary_on_every_item()
    {
        var source = Posts.Post("Source", tags: "a", id: 1);
        var other = Posts.Post("Other", tags: "a", id: 2, summary: new string('x', 5000) + " tail");

        var related = Posts.Service(new RelatedPostsOptions(), source, other).GetRelated(source).Single();

        Assert.True(related.Summary!.Length <= 303);
    }

    [Fact]
    public void The_cap_can_be_changed()
    {
        var source = Posts.Post("Source", tags: "a", id: 1);
        var other = Posts.Post("Other", tags: "a", id: 2, summary: "one two three four five six seven eight nine ten");

        var related = Posts.Service(new RelatedPostsOptions { SummaryMaxLength = 20 }, source, other).GetRelated(source).Single();

        Assert.True(related.Summary!.Length <= 23);
        Assert.EndsWith("...", related.Summary);
    }

    [Fact]
    public void A_cap_of_one_with_a_surrogate_pair_first_does_not_throw()
    {
        Assert.Equal("...", SummaryText.Cut("\uD83D\uDE00 emoji text here", 1));
    }

    [Fact]
    public void Rich_text_loses_its_tags_before_the_cut()
    {
        var html = "<p>" + string.Join(' ', Enumerable.Repeat("<strong>wordy</strong>", 100)) + "</p>";

        var cut = SummaryText.Cut(html, 50)!;

        Assert.DoesNotContain("<", cut);
        Assert.DoesNotContain(">", cut);
        Assert.True(cut.Length <= 53);
        Assert.Equal("Hello there", SummaryText.Cut("<p>Hello <em>there</em></p>", 300));
    }
}
