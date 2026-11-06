# BaryoDev.Umbraco.RelatedPosts

"Read next" related posts for Umbraco 17. Free, MIT.

It scores other published items under the same root by shared tags, same category, and (optional) title and summary similarity. Weights are yours. Picks you make by hand come first. Results are cached in memory and cleared when anything is published or unpublished. No external service, no database tables.

## Install

    dotnet add package BaryoDev.Umbraco.RelatedPosts

## Show the cards

In the view of a post:

    @await Component.InvokeAsync("RelatedPosts", new { content = Model })

Optional arguments: `count`, `heading`, `culture`. The card markup and `related-posts.css` follow the baryo.dev look. Override any of `--bdrp-radius`, `--bdrp-paper`, `--bdrp-line`, `--bdrp-ink`, `--bdrp-ink-soft`, `--bdrp-accent`, `--bdrp-font` to match your site. To change the markup, add your own `Views/Shared/Components/RelatedPosts/Default.cshtml`.

## Use the service

    public class MyController(IRelatedPostsService related) { ... related.GetRelated(content, 4) ... }

## Headless

    GET /related-posts/api/v1/{key}?count=3&culture=en-US

Returns `{ "items": [ { "id", "name", "url", "summary", "imageUrl", "manual" } ] }`. Published content only. Set `EnableApi` to false to turn it off. The route is open like the rest of your front end, so add your own rule if you need it private.

## Options

    "BaryoDev": {
      "RelatedPosts": {
        "DocumentTypeAliases": [ "blogPost" ],
        "TagsAlias": "tags",
        "CategoryAlias": "category",
        "SummaryAlias": "summary",
        "ImageAlias": "image",
        "ManualAlias": "relatedPicks",
        "TagWeight": 3,
        "CategoryWeight": 2,
        "EnableTextSimilarity": true,
        "TextWeight": 4,
        "Count": 3,
        "CacheMinutes": 60
      }
    }

Scoring, caching and every option are written up in [docs/design.md](docs/design.md).

## License

MIT
