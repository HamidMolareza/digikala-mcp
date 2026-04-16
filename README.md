# Digikala MCP

Digikala MCP is a .NET 10 Model Context Protocol server for searching, comparing, and inspecting Digikala products with AI-friendly tools and DTOs.

Current working notes for the next implementation phase live in:

- `docs/temp-review-notes.md`
- `docs/implementation-checklist.md`
- `docs/2026-04-16-android-apk-analysis.md`

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

## Offline API Contract Tests

Digikala can change response shapes without notice, so this repo keeps offline contract tests to catch breaking payload drift before it reaches runtime code paths.

- JSON fixtures live under `tests/Fixtures/ApiContracts/`.
- Fixture-backed assertions live in `tests/Digikala.Mcp.Application.Tests/OfflineApiContractTests.cs`.
- Changes that add a new upstream dependency, mapper path, or tool-facing field should add or update the matching offline contract fixture/test in the same PR.
- These tests are intentionally offline and deterministic; they validate the response paths and mapped DTO fields we depend on most.

## Live API Contract Tests

The repo also supports opt-in live contract tests that call Digikala directly and verify that current responses still deserialize into the existing Infrastructure models.

- Live tests live in `tests/Digikala.Mcp.Infrastructure.Tests/LiveDigikalaContractTests.cs`.
- They are disabled by default and run only when `DIGIKALA_RUN_LIVE_CONTRACT_TESTS=true`.
- Optional canary inputs can be overridden with `DIGIKALA_LIVE_PRODUCT_ID`, `DIGIKALA_LIVE_CATEGORY_SLUG`, and `DIGIKALA_LIVE_SEARCH_QUERY`.
- Use them for CI schedules, release checks, or after suspected upstream API drift.
- Some environments currently receive `403` HTML responses from Digikala edge even for anonymous read-only endpoints. Treat that as an egress/network constraint, not proof that the API requires login. Prefer running live probes from an allowed network path.

## Android APK Notes

- The analyzed Android APK (`Digikala.apk`, version `3.1.3-CB`) is a React Native app that embeds mobile host references such as `sirius.digikala.com` and `ravenclaw.digikala.com`.
- This MCP server should stay on the documented anonymous web API surface (`api.digikala.com` and `api-zakaria.digikala.com`) unless a separate investigation proves stable anonymous mobile routes worth adding.
- See `docs/2026-04-16-android-apk-analysis.md` for the evidence and scope decision.

## Runtime Paths

- Cache DB: `app-data/cache.db`
- Audit logs: `app-data/logs/audit-<date>.clef`
- Error logs: `app-data/logs/error-<date>.clef`

Runtime logs are local artifacts. Clear old `app-data/logs/*.clef` files before handing work off or starting a fresh implementation pass.

## Commands

```bash
dotnet restore Digikala.Mcp.slnx
dotnet build Digikala.Mcp.slnx --no-restore
dotnet test Digikala.Mcp.slnx --no-build
timeout 5s dotnet run --project src/Digikala.Mcp.Server --no-build
```

If the local machine is low on file-watcher capacity, use:

```bash
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false timeout 5s dotnet run --project src/Digikala.Mcp.Server --no-build
```

## Notes

- Logging uses Serilog and is configurable via `ServerLogging` settings or environment variables.
- The local cache uses SQLite through Entity Framework Core.
- Docs that drive the implementation live under `docs/`.
- Digipay credit is out of scope for this server and should not be reintroduced unless requirements change.
