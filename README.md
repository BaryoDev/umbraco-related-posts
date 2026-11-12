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

Returns `{ "items": [ { "id", "name", "url", "summary", "imageUrl", "manual" } ] }`. Published content only. Unknown, unpublished, protected or wrong-type sources return 404. Set `EnableApi` to false to turn it off. The route is open like the rest of your front end, so add your own rule if you need it private.

## Options

    "BaryoDev": {
      "RelatedPosts": {
        "DocumentTypeAliases": [ "blogPost" ],
        "TagsAlias": "tags",
        "CategoryAlias": "category",
        "SummaryAlias": "summary",
        "ImageAlias": "image",
        "DateAlias": "publishDate",
        "ManualAlias": "relatedPicks",
        "TagWeight": 3,
        "CategoryWeight": 2,
        "EnableTextSimilarity": true,
        "TextWeight": 4,
        "Count": 3,
        "MaxCount": 20,
        "AllowProtected": false,
        "CacheMinutes": 60
      }
    }

`DateAlias` names the date property shown on each card. It is used when the post has a value for it, and the card falls back to the update date otherwise. Leave it empty to always show the update date. `AllowProtected` is false by default, so items under public access protection never show. `MaxCount` caps any requested count. The stylesheet is served from `/App_Plugins/BaryoDev.RelatedPosts/related-posts.css`, and the link follows the site's path base. Set `StylesheetPath` to another path, or to an empty string for no link, when you bring your own styles. The link is written once per page. Summaries are cut to `SummaryMaxLength` characters (default 300, 0 for no cut) at a word edge, in the cards and the API. The cache keeps at most `MaxCacheEntries` entries (default 10000: one per root and culture, plus one per source post and culture viewed within `CacheMinutes`). Past that it drops about 5 percent of its entries to make room, those lists are computed again, and a warning is logged once. The limit is read at start. Text similarity suits English only. `SummaryAlias` reads text properties only, so point it at a text area, not rich text or blocks.

An item at the top of the tree (no section above it) gets no related posts. Posts need to sit under a Home or section page.

Leave `DocumentTypeAliases` empty and the Home and section pages can show up as cards. Set it (for example to `blogPost`) to keep them out.

Changing `TagsAlias`, the weights, `AllowProtected` or other options at runtime clears the cache.

Scoring, caching and every option are written up in [docs/design.md](docs/design.md).

## License

MIT
