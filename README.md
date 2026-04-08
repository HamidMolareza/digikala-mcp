# Digikala MCP

Digikala MCP is a .NET 10 Model Context Protocol server for searching, comparing, and inspecting Digikala products with AI-friendly tools and DTOs.

## Architecture

- `src/Digikala.Mcp.Domain`: entities, enums, interfaces
- `src/Digikala.Mcp.Application`: DTOs, mapping, reusable services
- `src/Digikala.Mcp.Infrastructure`: HTTP client, EF-backed SQLite cache, resilience, configuration
- `src/Digikala.Mcp.Server`: MCP host, Serilog setup, tool wrappers

## Main Tools

- `search_products`
- `get_product_overview`
- `get_product_detail`
- `get_product_price_chart`
- `get_product_reviews`
- `get_product_questions`
- `get_product_recommendations`
- `get_best_selling`
- `get_incredible_offers`
- `get_trending_products`
- `get_category_landing`
- `browse_by_tag`
- `autocomplete`
- `get_brand_page`
- `get_seller_vouchers`

## Search Optimization

`search_products` supports AI-oriented ranking and filtering:

- `minRating`
- `minReviewCount`
- `onlyAvailable`
- `minPriceRials`
- `maxPriceRials`
- `limit`
- `prioritizeBestMatch`

The result includes `BestMatch`, `FilteredCount`, `AppliedFilters`, and per-product `SearchScore`.

## Category Resolution

Digikala's documented public product search is category-scoped. This server therefore resolves categories before calling `search_products`:

- Explicit `categorySlug` is used as-is.
- Known shopping intents are mapped with alias rules, including `mobile-phone`, `hair-shampoo`, `power-bank`, and `cell-phone-data-cable`.
- If no alias matches, the server uses `autocomplete` category hints as a fallback.
- If the category still cannot be resolved, the tool returns a structured validation error instead of attempting a placeholder global search.

This keeps MCP shopping flows inside Digikala data while still supporting natural-language requests such as `iphone`, `power bank 20000`, or `type-c cable`.

The search and recommendation workflows also validate common bad inputs before hitting upstream:

- empty query or need description
- `page`/`limit` values below `1`
- `minRating` outside `0..5`
- negative count or price filters
- `minPriceRials > maxPriceRials`

## Resilience Notes

- The HTTP client retries transient upstream failures once.
- Stale cache can be served when fresh upstream requests fail.
- The per-host circuit breaker now counts only transient failures such as timeout, `429`, and `5xx` responses.
- Non-transient client errors like `404` do not open the circuit breaker, which prevents bad category guesses from poisoning later requests.

## Runtime Paths

- Cache DB: `app-data/cache.db`
- Audit logs: `app-data/logs/audit-<date>.clef`
- Error logs: `app-data/logs/error-<date>.clef`

## Commands

```bash
dotnet restore Digikala.Mcp.slnx
dotnet build Digikala.Mcp.slnx --no-restore
dotnet test Digikala.Mcp.slnx --no-build
timeout 5s dotnet run --project src/Digikala.Mcp.Server --no-build
```

## Notes

- Logging uses Serilog and is configurable via `ServerLogging` settings or environment variables.
- The local cache uses SQLite through Entity Framework Core.
- Docs that drive the implementation live under `docs/`.
