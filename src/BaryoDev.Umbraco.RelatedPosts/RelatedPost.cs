namespace BaryoDev.Umbraco.RelatedPosts;

public sealed record RelatedPost(
    Guid Key,
    string Name,
    string Url,
    string? Summary,
    string? ImageUrl,
    double Score,
    bool Manual,
    DateTime UpdateDate)
{
    /// <summary>The date shown on the card: the configured date property when set, else UpdateDate.</summary>
    public DateTime DisplayDate { get; init; } = UpdateDate;
}
