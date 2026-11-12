# Search relevance and endpoint drift (2026-11-11)

## What was wrong

- The `search_products` sort ids in the tool description were wrong. The model sent `25` expecting best-selling, which Digikala treats as fastest shipping, and `1` expecting relevance, which is newest. This made results look unrelated to the keyword.
- Rating, review-count, availability and price filters ran locally on a single 20-item page, so most relevant products were dropped.
- `SearchScore` was dominated by rating. Digikala now reports ratings on a 0-100 scale, so `minRating` (0-5) also stopped filtering anything.
- `recommend_products_for_need` searched with the first words of the need sentence and `sort=1` (newest), and often returned nothing.
- Digikala retired three endpoints, which now return `{"status":404}`: `/v1/autocomplete/`, `/v1/search/` and `/v1/categories/{slug}/`.

## Endpoints now used

| Purpose | Endpoint | Notes |
|---|---|---|
| Category search | `/v1/categories/{slug}/search/?q=&page=&sort=` | `price[min]` and `price[max]` work only when both are sent; `has_selling_stock=1` limits to in-stock items. |
| Global search | `/discovery/api/v2/search?q=&page=&sort=` | Products sit in `data.widgets[type=vertical_product_listing].data.widgets[type=product].data`. The price filter is not reliable here, so it is also applied locally. |
| Autocomplete | `/discovery/api/v2/autocomplete?q=` | `data.auto_complete[]` with `type` (`query` or `category`); category slug comes from `url.url` (`/search/category-{slug}/`). |
| Category landing | `/v1/categories/{slug}/search/?sort=7` | Built from best sellers. Sub-categories are not available; `/v1/category-tree/` is a flat list with no parent links. |

Live sort ids: 22 most relevant (default), 4 most viewed, 1 newest, 7 best-selling, 20 cheapest, 21 most expensive, 25 fastest shipping, 27 buyers' pick, 29 featured.

## Known remaining issues

- The live contract test `GetMagnetHomePageAsync_ShouldDeserializePostsEnvelope` fails: `data.posts[].products[].price` no longer matches `PriceModel`. This change does not touch that path.
- Category alias rules match substrings (for example `phone` inside `headphone`), so some queries can still be routed to the wrong category.
- `docs/request-response.api-summary.yaml` still documents the retired endpoints.
