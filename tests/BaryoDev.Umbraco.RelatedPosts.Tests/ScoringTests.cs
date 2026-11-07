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
        var options = new RelatedPostsOptions { TagWeight = 5, CategoryWeight = 1, Count = 10 };

        var related = Posts.Service(options, source, both, oneTag, categoryOnly, nothing).GetRelated(source);

        Assert.Equal(["Both", "One tag", "Category only"], related.Select(r => r.Name));
        Assert.Equal([11d, 5d, 1d], related.Select(r => r.Score));
    }

    [Fact]
    public void Json_array_tag_string_is_parsed_not_split_on_commas()
    {
        var source = Posts.Post("Source", tags: "[\"news, events\",\"umbraco\"]", id: 1);
        var shared = Posts.Post("Shared", tags: "[\"news, events\"]", id: 2);
        var split = Posts.Post("Split", tags: "events", id: 3);
        var options = new RelatedPostsOptions { TagWeight = 3, Count = 10 };

        var related = Posts.Service(options, source, shared, split).GetRelated(source);

        var only = Assert.Single(related);
        Assert.Equal("Shared", only.Name);
        Assert.Equal(3d, only.Score);
    }

    [Fact]
    public void Comma_separated_tags_still_work()
    {
        var source = Posts.Post("Source", tags: "a, b", id: 1);
        var other = Posts.Post("Other", tags: "b,c", id: 2);

        var related = Posts.Service(new RelatedPostsOptions(), source, other).GetRelated(source);

        Assert.Equal(3d, Assert.Single(related).Score);
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
