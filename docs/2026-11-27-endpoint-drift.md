# Endpoint drift (2026-11-27)

Two weeks after the 2026-11-11 fix, Digikala retired more routes. The live tools `search_products` (any query that resolved to a category), `get_category_landing`, `get_product_detail`, `get_product_overview`, `get_brand_page` and `get_incredible_offers` all failed or returned empty results.

The replacement routes were found by loading the matching pages on digikala.com in a browser and reading the API calls the site makes.

## Route changes

| Purpose | Old route (now) | New route |
|---|---|---|
| Category search, category landing | `/v1/categories/{slug}/search/` (404) | `/discovery/api/v2/categories/{slug}/products?q=&page=&sort=` |
| Product detail | `/v2/product/{id}/` (403 WAF HTML page) | `/product/v1/products/{id}/` |
| Incredible offers | `/v1/incredible-offers/` (only `incredible_banner_slider`), `/v1/incredible-offers/products/` (404) | `/amazing/v1/page/0?no_widgets=1&page=` |
| Brand page | `/v1/brands/{slug}/premium/` (404) | `/discovery/api/v1/brands/{slug}/products?page=&sort=` |
| Browse by tag | `/v1/tags/{slug}/` (404) | `/discovery/api/v1/product-tags/{slug}/products?page=` (same flat `products` + `pager` shape) |

## Response shapes

- Category products and amazing listings use the same envelope as global search: `data.widgets[type=vertical_product_listing].data` holds `pager`, `filters`, `sort_options` and `category`; products are in its `widgets[type=product].data`. `ProductListing.Unwrap` handles both this envelope and the older flat `data.products`.
- The discovery listing's `category` has `id` and titles but no `code`, so the category landing falls back to the requested slug.
- `price[min]`/`price[max]`, `has_selling_stock` and `sort` still work on the category route.
- Amazing listing items carry `price` and `image` at the top level instead of `default_variant.price` and `images`.
- The brand route returns `data.brand` (same fields as the old premium page) plus a flat `products` list. `get_brand_page` now makes two calls: `sort=1` for newest and `sort=7` for best-selling, each capped at 10 products.
- Product detail keeps the old `data.product` shape with extra fields. `default_variant.price.discount_percent` can be a decimal (`0.0`), so `PriceModel.DiscountPercent` is now `double?` and `GetInt32OrNull` rounds decimals. The Magnet home-page contract test reported on 2026-11-11 failed because, with the `X-Web-Optimize-Response` header the server sends, a product without a price has `"price": []`. `EmptyArrayAsNullConverter` reads that as null; `selling_price` can also be `null`, so `PriceModel.SellingPrice` is now `long?`.

## Known remaining issues

- `/v1/category-tree/` returns 404. No replacement was found yet; it is not used by any MCP tool.
- `/v1/product/{id}/price-chart/` returned 429 during heavy probing; the route still exists.
- `/v2/` (homepage) returns 404; it is not used by any MCP tool.
