using BaryoDev.Umbraco.RelatedPosts.Scoring;

namespace BaryoDev.Umbraco.RelatedPosts.Tests;

public class ScoringTests
{
    [Fact]
    public void Shared_tags_and_category_score_by_weight()
    {
        var source = Posts.Post("Source", tags: "a,b", category: "c1", id: 1);
        var both = Posts.Post("Both", tags: "a,b", category: "c1", id: 2);
        var oneTag = Posts.Post("One tag", tags: "a", category: "c2", id: 3);
        var categoryOnly = Posts.Post("Category only", tags: "z", category: "c1", id: 4);
        var nothing = Posts.Post("Nothing", tags: "z", category: "c2", id: 5);
        var options = new RelatedPostsOptions { TagWeight = 5, CategoryWeight = 1, EnableTextSimilarity = false, Count = 10 };

        var related = Posts.Service(options, source, both, oneTag, categoryOnly, nothing).GetRelated(source);

        Assert.Equal(["Both", "One tag", "Category only"], related.Select(r => r.Name));
        Assert.Equal([11d, 5d, 1d], related.Select(r => r.Score));
    }

    [Fact]
    public void Invariant_content_is_related_when_no_culture_is_resolved()
    {
        var source = Posts.Post("Source", tags: "a", id: 1);
        var other = Posts.Post("Other", tags: "a", id: 2);
        var cultures = new List<string?>();
        var options = new RelatedPostsOptions { EnableTextSimilarity = false };

        var related = Posts.Service(options, cultures, source, other).GetRelated(source);

        Assert.Equal(["Other"], related.Select(r => r.Name));
        Assert.Equal(["en-US"], cultures);
    }

    [Fact]
    public void Json_array_tag_string_is_parsed_not_split_on_commas()
    {
        var source = Posts.Post("Source", tags: "[\"umbraco\",\"news, events\"]", id: 1);
        var shared = Posts.Post("Shared", tags: "[\"news, events\"]", id: 2);
        var plain = Posts.Post("Plain", tags: "umbraco", id: 3);
        var split = Posts.Post("Split", tags: "events", id: 4);
        var options = new RelatedPostsOptions { TagWeight = 3, EnableTextSimilarity = false, Count = 10 };

        var related = Posts.Service(options, source, shared, plain, split).GetRelated(source);

        // The old comma split leaves quotes on "umbraco", so Plain would score 0.
        Assert.Equal(["Plain", "Shared"], related.OrderBy(r => r.Name).Select(r => r.Name));
        Assert.All(related, r => Assert.Equal(3d, r.Score));
    }

    [Theory]
    [InlineData("[")]
    [InlineData("[\"a\"")]
    [InlineData("[1,2]")]
    [InlineData("[null]")]
    public void Malformed_json_tags_fall_back_without_throwing(string tags)
    {
        var source = Posts.Post("Source", tags: tags, id: 1);
        var other = Posts.Post("Other", tags: tags, id: 2);
        var options = new RelatedPostsOptions { EnableTextSimilarity = false };

        var related = Posts.Service(options, source, other).GetRelated(source);

        Assert.NotNull(related);
    }

    [Fact]
    public void Comma_separated_tags_still_work()
    {
        var source = Posts.Post("Source", tags: "a, b", id: 1);
        var other = Posts.Post("Other", tags: "b,c", id: 2);
        var options = new RelatedPostsOptions { TagWeight = 4, EnableTextSimilarity = false };

        var related = Posts.Service(options, source, other).GetRelated(source);

        Assert.Equal(4d, Assert.Single(related).Score);
    }

    [Fact]
    public void Text_similarity_ignores_stop_words_and_short_tokens()
    {
        Assert.Equal(["umbraco", "caching"], TextSimilarity.Tokens("The Umbraco and caching of it"));
    }

    [Fact]
    public void Text_similarity_ranks_shared_words_above_none()
    {
        var scores = TextSimilarity.Score(
            ["umbraco", "caching"],
            [["umbraco", "caching"], ["gardening", "tomatoes"]]);

        Assert.True(scores[0] > 0.99);
        Assert.Equal(0d, scores[1]);
    }
}
