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
