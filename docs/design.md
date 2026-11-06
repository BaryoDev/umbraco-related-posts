# Design

"Read next" related posts for an Umbraco 17 content item. Free, MIT, no database tables, no external service.

## Scoring

For a source item, every candidate gets a score. Candidates are published items under the same root, limited to the configured document type aliases, minus the source itself.

- Tags: number of shared tags, from the property at `TagsAlias`, times `TagWeight`. Matching ignores case.
- Category: `CategoryWeight` if the value at `CategoryAlias` matches. A content picker compares keys, text compares case-insensitively.
- Text: optional, off by default. Cosine similarity of TF-IDF vectors built from title and summary (`SummaryAlias`), times `TextWeight`. The document frequency comes from the candidate set, so there is no corpus to ship. Stop words are a small built-in English list. Terms shorter than 3 characters are dropped.
- Manual picks: items in the picker at `ManualAlias` come first, in picker order, then the scored items.

Items with a score of 0 are dropped unless `IncludeZeroScore` is true. Ties break on newest `UpdateDate`, then name, so output is stable. Result size is `Count`.

## Caching

The candidate list per root and culture is cached, and the ranked keys per source and culture. Urls are built per request, so the request domain is never cached. Misses are single flight. The cache is package-owned and bounded. Preview requests skip it. The culture is resolved first (argument, then the ambient variation context). Content, media, domain and public access cache refresher notifications cancel a shared token, which drops every entry and also covers load-balanced servers. This is coarse on purpose. Related lists depend on the whole candidate set, so a finer rule would be wrong more often than it is fast. Entries also expire after `CacheMinutes`.

## Surface

- `IRelatedPostsService.GetRelated(IPublishedContent, int?)` returns `RelatedPost` records (key, name, url, summary, image, score, manual).
- View component `RelatedPosts`, called as `@await Component.InvokeAsync("RelatedPosts", new { content = Model })`, renders the card partial.
- Endpoint `GET /related-posts/api/v1/{key}?count=3` returns keys, names, urls for headless sites. It only reads published content and returns 404 for unknown, unpublished, protected or wrong-type sources.

## Options

Section `BaryoDev:RelatedPosts`:

| Key | Default |
| --- | --- |
| DocumentTypeAliases | empty, which means any type |
| TagsAlias | `tags` |
| CategoryAlias | `category` |
| SummaryAlias | `summary` |
| ImageAlias | `image` |
| ManualAlias | empty, off |
| TagWeight | 3 |
| CategoryWeight | 2 |
| TextWeight | 4, used only if `EnableTextSimilarity` |
| EnableTextSimilarity | false |
| Count | 3 |
| CacheMinutes | 60 |
| MaxCount | 20, cap for any requested count |
| AllowProtected | false, protected items are left out |
| IncludeZeroScore | false |
| EnableApi | true |

## Paid-tier candidates

Not built. Ideas only.

- Editor dashboard showing why each item was picked, with per-site weight tuning.
- Click tracking and a feedback loop that adjusts weights.
- Embedding based similarity with a bring-your-own model endpoint.
- Per-culture stop word lists and stemming.
- Scheduled "newsletter" digest of related posts.
