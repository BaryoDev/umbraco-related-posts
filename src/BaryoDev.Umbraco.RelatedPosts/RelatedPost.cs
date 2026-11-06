namespace BaryoDev.Umbraco.RelatedPosts;

public sealed record RelatedPost(
    Guid Key,
    string Name,
    string Url,
    string? Summary,
    string? ImageUrl,
    double Score,
    bool Manual,
    DateTime UpdateDate);
