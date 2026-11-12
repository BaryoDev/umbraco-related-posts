# Changelog

## Unreleased

- New option `MaxCacheEntries` (default 10000) for the cache limit, with a warning logged once when the cache is full.
- Summaries are cut to `SummaryMaxLength` characters (default 300) at a word edge, in the cards and the API.
- The stylesheet link follows the site's path base, can be changed or turned off with `StylesheetPath`, and is written once per page.
- Content type and language changes now clear the cache.

## 0.2.0

The card date prefers the `publishDate` property (option `DateAlias`) when a post has one, and falls back to the update date. Before, imported posts showed the import date.

## 0.1.0

First public release for Umbraco 17. A Read next row. Posts are ranked by shared tags and category, with optional text similarity and manual picks. IncludeZeroScore also fills the row with posts that share nothing. Ranked posts are limited to the document types you configure (any type when the list is empty). Manual picks are shown whatever their type.
