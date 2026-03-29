# Digikala MCP Tool — Application Requirements

> **Purpose:** Build an MCP (Model Context Protocol) server that exposes Digikala's public web API as a set of AI-callable tools, enabling an LLM to search, browse, compare, and inspect products on Digikala.

---

## 1. Project Overview

| Item | Value |
|---|---|
| **Runtime** | .NET 10 |
| **MCP SDK** | `ModelContextProtocol` v1.1.0 (NuGet) |
| **Architecture** | Multi-layer (Domain → Application → Infrastructure → Server) |
| **Upstream API** | `https://api.digikala.com` (primary), `https://api-zakaria.digikala.com` (feature flags) |
| **Target consumers** | AI assistants (Claude, GPT, etc.) via MCP |
| **Language / locale** | Code, comments, commits in English; product content is Persian (Farsi) |

---

## 2. Functional Requirements

### 2.1 MCP Tools (AI-Facing)

Each tool is a single MCP callable exposed to the AI. The AI receives compact DTOs — not the raw upstream JSON.

| # | Tool Name | Description | Upstream Endpoint(s) |
|---|---|---|---|
| T-01 | `search_products` | Search products by keyword within an optional category, with filters, sorting, and pagination. | `GET /v1/categories/{slug}/search/`, `GET /fresh/v1/categories/{slug}/search/` |
| T-02 | `get_product_detail` | Get full product information for a single product ID, including variants, images, price, seller, specs. | `GET /v2/product/{id}/`, `GET /fresh/v1/product/{id}/` |
| T-03 | `get_product_price_chart` | Get historical price data for a product. | `GET /v1/product/{id}/price-chart/` |
| T-04 | `get_product_reviews` | Get paginated user reviews/comments for a product. | `GET /v1/product/{id}/comments/`, `GET /v1/rate-review/products/{id}/` |
| T-05 | `get_product_questions` | Get paginated Q&A for a product. | `GET /v1/product/{id}/questions/` |
| T-06 | `get_product_recommendations` | Get recommended/similar products. | `GET /v1/product/{id}/recommendation/`, `GET /fresh/v1/product/{id}/recommendation/` |
| T-07 | `get_best_selling` | Get best-selling products, optionally by category and time window. | `GET /v1/best-selling/` |
| T-08 | `get_incredible_offers` | Get products currently on incredible/lightning deals. | `GET /v1/incredible-offers/`, `GET /v1/incredible-offers/products/` |
| T-09 | `get_trending_products` | Get trending products. | `GET /v1/recommendation/trending/` |
| T-10 | `get_category_landing` | Get a category landing page (subcategories, popular brands, highlights). | `GET /v1/categories/{slug}/` |
| T-11 | `browse_by_tag` | Browse products by SEO tag (e.g. "football-boots", "iphone-16"). | `GET /v1/tags/{slug}/` |
| T-12 | `autocomplete` | Get search autocomplete suggestions and trends for a query prefix. | `GET /v1/autocomplete/` |
| T-13 | `get_brand_page` | Get a premium brand landing page. | `GET /v1/brands/{slug}/premium/` |
| T-14 | `get_seller_vouchers` | Get seller vouchers/promotions for a product. | `GET /v1/product/{id}/seller-vouchers/` |
| T-15 | `get_digipay_credit` | Get Digipay credit/installment options for a product. | `GET /v1/product/{id}/digipay-credit/` |

### 2.2 Infrastructure-Layer API Coverage

The infrastructure layer must provide a typed HTTP client that covers **all 40 observed endpoints** (see `request-response.api-summary.yaml`), including those not directly exposed as MCP tools:

| Group | Endpoints |
|---|---|
| **Session / Bootstrap** | `GET /v1/user/init/`, `GET /fresh/v1/init/`, `GET /v2/` |
| **Dictionaries** | `GET /v1/dictionaries/`, `GET /fresh/v1/dictionaries/` |
| **Feature Flags** | `GET /api/features/{sdk_key}` |
| **Dynamic Landing** | `GET /v1/dynamic-landing/mobile/`, `GET /v1/dynamic-landing/widget/{id}/` |
| **Magnet / Content** | `GET /v1/magnet/home-page/`, `GET /v1/magnet/notification-center/announcement/message/`, `GET /v1/magnet/related-posts/{id}/` |
| **Product (main)** | `GET /v2/product/{id}/`, `GET /fresh/v1/product/{id}/` |
| **Product (sub-resources)** | comments, price-chart, questions, recommendation, cpc, pindo, seller-vouchers, digipay-credit |
| **Search / Browse** | category search (v1 + fresh), tags, providers-products, incredible-offers/products |
| **Recommendations** | home, trending, selling-and-sales, personalized-category |
| **Best Selling** | `GET /v1/best-selling/` |
| **Autocomplete** | `GET /v1/autocomplete/` |
| **Brands** | `GET /v1/brands/{slug}/premium/` |
| **Comments Overview** | `GET /v1/comments-overview/{id}/user-feedback/` |
| **Sentry (telemetry)** | `POST /api/3/envelope/` — included for completeness but NOT called by the MCP server |

### 2.3 Request / Response Models vs AI DTOs

- **Full models** — Every upstream endpoint gets a pair of request-options class and response model that mirrors the upstream JSON shape 1:1. These live in the Infrastructure layer.
- **AI DTOs** — A separate set of compact, flattened DTOs live in the Application (or Domain) layer. These are what MCP tools return. They strip unnecessary nesting, convert Persian labels where needed, add computed fields (e.g. discount percentage), and omit internal/tracking fields.

Example mapping:

```
Upstream response (v2/product/{id}/)
  └─ data.product.title_fa          →  ProductDetailDto.Title
  └─ data.product.images[0].url     →  ProductDetailDto.MainImageUrl
  └─ data.product.default_variant.price.selling_price  →  ProductDetailDto.PriceRials
  └─ data.product.rating.rate       →  ProductDetailDto.Rating
  └─ data.product.rating.count      →  ProductDetailDto.ReviewCount
  └─ data.data_layer  →  (omitted from DTO)
  └─ data.bigdata_tracker_data  →  (omitted from DTO)
```

---

## 3. Non-Functional Requirements

### 3.1 Caching

All caching is **local to the MCP server process** using an embedded SQLite database.

| Tier | TTL | Examples |
|---|---|---|
| **Long-term** (rarely changes) | 24 hours | Product title, brand, category tree, dictionary data (states, cities, mega menu), product images, specs/attributes, category slugs |
| **Medium-term** (changes moderately) | 1 hour | Product description, seller information, review summary/aggregates, recommendation lists, best-selling lists, brand page content |
| **Short-term** (changes frequently) | 5 minutes | Product price, stock/availability, individual reviews page, rating score, discount/offer status, incredible-offers listings, autocomplete results |
| **No cache** | 0 | User session init (`/v1/user/init/`), feature flags, Sentry telemetry |

Cache design:

- Cache key = normalized endpoint path + sorted query parameters (excluding tracking params like `utm_*`).
- SQLite table: `cache_entries (key TEXT PRIMARY KEY, value BLOB, tier TEXT, expires_at INTEGER, created_at INTEGER)`.
- Background cleanup of expired entries on a configurable interval (default: every 10 minutes).
- Cache can be force-invalidated per-key or per-tier via an internal method (not exposed as MCP tool).
- Cache is **read-through**: on miss, call upstream → store → return; on hit, return stored value.

### 3.2 HTTP Client

- Use `HttpClient` with `IHttpClientFactory` for connection pooling and DNS rotation.
- Default request headers: `X-Web-Optimize-Response: 1`, `X-Web-Client: desktop`, `X-Web-Client-Id: web`.
- Retry policy: 1 automatic retry on transient failures (5xx, timeout) with 500ms delay.
- Timeout: 10 seconds per request.
- Rate limiting: client-side token bucket (configurable, default 30 requests/second) to avoid upstream throttling.
- User-Agent: configurable, default to a browser-like string.

### 3.3 Resilience

- If upstream returns non-200, return a structured error to the AI with status code and message.
- If cache is populated and upstream fails, serve stale cache with a warning flag in the DTO.
- Circuit breaker: if 5 consecutive failures to the same host occur within 30 seconds, open the circuit for 60 seconds and serve cache-only.

### 3.4 Performance

- Target p95 tool response time: < 2 seconds for cached data, < 5 seconds for cache-miss with upstream call.
- SQLite WAL mode for concurrent read/write.
- Minimize JSON deserialization allocations using `System.Text.Json` source generators.

### 3.5 Configuration

All configurable via `appsettings.json` and/or environment variables:

| Key | Default | Description |
|---|---|---|
| `Digikala:BaseUrl` | `https://api.digikala.com` | Primary API base URL |
| `Digikala:FeaturesBaseUrl` | `https://api-zakaria.digikala.com` | Feature flags API base URL |
| `Digikala:TimeoutSeconds` | `10` | HTTP request timeout |
| `Digikala:MaxRequestsPerSecond` | `30` | Client-side rate limit |
| `Digikala:UserAgent` | `Mozilla/5.0 ...` | User-Agent header |
| `Cache:DatabasePath` | `./cache.db` | SQLite database file path |
| `Cache:LongTermTtlMinutes` | `1440` | 24 hours |
| `Cache:MediumTermTtlMinutes` | `60` | 1 hour |
| `Cache:ShortTermTtlMinutes` | `5` | 5 minutes |
| `Cache:CleanupIntervalMinutes` | `10` | Expired-entry purge interval |

### 3.6 Security & Privacy

- No authentication tokens are stored or transmitted; all observed endpoints are publicly accessible.
- Cookie values from upstream `Set-Cookie` headers (PHPSESSID, tracker cookies) are managed per HTTP client session but never exposed to the AI.
- No user PII is collected or cached.

### 3.7 Logging & Observability

- Structured logging via `Microsoft.Extensions.Logging` (Serilog-compatible).
- Log levels: upstream HTTP calls at `Debug`, cache hits/misses at `Debug`, tool invocations at `Information`, errors at `Warning`/`Error`.
- No Sentry integration (the upstream Sentry endpoint is not called).

---

## 4. Software Architecture

### 4.1 Solution Structure

```
Digikala.Mcp.sln
│
├── src/
│   ├── Digikala.Mcp.Domain/              # Entities, value objects, enums, interfaces
│   ├── Digikala.Mcp.Application/         # AI DTOs, tool definitions, mapping logic
│   ├── Digikala.Mcp.Infrastructure/      # HTTP clients, response models, caching, API wrappers
│   └── Digikala.Mcp.Server/              # MCP server host, DI composition root, Program.cs
│
├── tests/
│   ├── Digikala.Mcp.Application.Tests/
│   └── Digikala.Mcp.Infrastructure.Tests/
│
├── app-requirements.md                    # This file
└── request-response.api-summary.yaml      # Upstream API reference
```

### 4.2 Layer Responsibilities

#### Domain Layer (`Digikala.Mcp.Domain`)
- Defines core entities: `Product`, `Category`, `Brand`, `Review`, `Question`, `PricePoint`, `Offer`.
- Defines value objects: `Money`, `Rating`, `ImageUrl`, `Pagination`.
- Defines interfaces: `IDigikalaApiClient`, `ICacheStore`.
- Defines enums: `SortOption`, `CacheTier`, `ProductAvailability`.
- Zero external dependencies (no NuGet packages beyond the SDK).

#### Application Layer (`Digikala.Mcp.Application`)
- Defines AI-facing DTOs: `ProductSummaryDto`, `ProductDetailDto`, `SearchResultDto`, `ReviewDto`, `QuestionDto`, `PriceChartDto`, `CategoryDto`, `OfferDto`, `AutocompleteDto`, `BrandPageDto`.
- Defines MCP tool handler classes — one class per tool (T-01 through T-15).
- Contains mapping logic from infrastructure response models → AI DTOs.
- References Domain; does not reference Infrastructure.

#### Infrastructure Layer (`Digikala.Mcp.Infrastructure`)
- `DigikalaHttpClient` — typed HTTP client wrapping all 40 endpoints.
- Per-endpoint request-option classes and full response models (1:1 with upstream JSON).
- `SqliteCacheStore` — implements `ICacheStore` from Domain.
- Configuration binding classes (`DigikalaOptions`, `CacheOptions`).
- Retry/circuit-breaker policies (using Polly or built-in `HttpClient` resilience).
- References Domain.

#### Server Layer (`Digikala.Mcp.Server`)
- `Program.cs` — composition root, DI registration, MCP server startup.
- Registers all MCP tools from Application layer.
- Configures `IHttpClientFactory`, logging, cache, options.
- References Application and Infrastructure.

### 4.3 Data Flow

```
AI (Claude/GPT)
    │
    ▼
MCP Protocol (stdio/SSE)
    │
    ▼
Digikala.Mcp.Server
    │
    ▼
Application Layer (Tool Handler)
    │  ← maps response models → AI DTOs
    ▼
Infrastructure Layer
    ├── SqliteCacheStore ──► SQLite (cache.db)
    │       │ cache miss
    │       ▼
    └── DigikalaHttpClient ──► api.digikala.com
```

### 4.4 Dependency Injection Graph

```
IServiceCollection
  ├── AddSingleton<ICacheStore, SqliteCacheStore>()
  ├── AddHttpClient<IDigikalaApiClient, DigikalaHttpClient>(configure)
  │       ├── AddStandardResilienceHandler()   // retry + circuit breaker
  │       └── Configure default headers
  ├── AddTransient<SearchProductsTool>()
  ├── AddTransient<GetProductDetailTool>()
  ├── ... (all 15 tools)
  └── AddMcpServer(options => { options.AddTools(...) })
```

---

## 5. AI Tool Specifications

### 5.1 Tool Input/Output Contracts

Every tool follows a consistent pattern:

```csharp
// Input: a record with [Description] attributes for MCP schema generation
public record SearchProductsInput(
    [Description("Search keyword in Persian or English")]
    string Query,

    [Description("Category slug, e.g. 'mobile-phone'. Null for all categories.")]
    string? CategorySlug = null,

    [Description("Page number, starting from 1")]
    int Page = 1,

    [Description("Sort option: 1=relevance, 4=most-popular, 7=cheapest, 8=most-expensive, 25=best-selling, 27=newest")]
    int? Sort = null
);

// Output: a DTO with only AI-relevant fields
public record SearchResultDto(
    List<ProductSummaryDto> Products,
    int TotalCount,
    int CurrentPage,
    int TotalPages,
    List<string> AvailableFilters
);
```

### 5.2 ProductSummaryDto (used across search, best-selling, recommendations, offers)

| Field | Type | Source | Cache Tier |
|---|---|---|---|
| `Id` | `long` | `data.products[].id` | long-term |
| `Title` | `string` | `data.products[].title_fa` | long-term |
| `EnglishTitle` | `string?` | `data.products[].title_en` | long-term |
| `MainImageUrl` | `string` | `data.products[].images[0].url` | long-term |
| `PriceRials` | `long` | `data.products[].default_variant.price.selling_price` | short-term |
| `OriginalPriceRials` | `long?` | `data.products[].default_variant.price.rrp_price` | short-term |
| `DiscountPercent` | `int?` | computed | short-term |
| `Rating` | `double?` | `data.products[].rating.rate` | short-term |
| `ReviewCount` | `int` | `data.products[].rating.count` | short-term |
| `CategorySlug` | `string` | `data.products[].category.code` | long-term |
| `IsAvailable` | `bool` | `data.products[].status` == "marketable" | short-term |
| `Url` | `string` | `data.products[].url.uri` | long-term |

### 5.3 ProductDetailDto (used by `get_product_detail`)

All fields from `ProductSummaryDto` plus:

| Field | Type | Source | Cache Tier |
|---|---|---|---|
| `Description` | `string` | `data.product.expert_reviews.description` | medium-term |
| `Brand` | `string` | `data.product.brand.title_fa` | long-term |
| `BrandSlug` | `string` | `data.product.brand.code` | long-term |
| `Seller` | `string` | `data.product.default_variant.seller.title` | medium-term |
| `SellerRating` | `double?` | `data.product.default_variant.seller.rating` | medium-term |
| `Warranty` | `string?` | `data.product.default_variant.warranty.title_fa` | medium-term |
| `Specifications` | `List<SpecDto>` | `data.product.specifications` | long-term |
| `ImageUrls` | `List<string>` | `data.product.images[].url` | long-term |
| `Variants` | `List<VariantDto>` | `data.product.variants` | short-term |
| `ProsCons` | `ProsConsDto?` | `data.product.expert_reviews` | medium-term |

### 5.4 Tool Error Handling

All tools return a consistent error shape when upstream fails:

```json
{
  "error": true,
  "code": "UPSTREAM_ERROR",
  "message": "Digikala API returned HTTP 500",
  "stale_data": { ... }  // optional: last cached data if available
}
```

Error codes: `UPSTREAM_ERROR`, `RATE_LIMITED`, `NOT_FOUND`, `INVALID_INPUT`, `CIRCUIT_OPEN`.

---

## 6. Caching Strategy — Detailed Field Classification

### 6.1 Long-Term Cache (24h)

Data that is essentially static or changes very rarely:

- Product titles (Persian and English)
- Product images
- Product specifications / attributes
- Brand names and slugs
- Category tree / slugs / hierarchy
- Dictionary data (states, cities, mega menu, user jobs, districts, seo content)
- SEO metadata
- Product URL/slug

### 6.2 Medium-Term Cache (1h)

Data that changes occasionally:

- Product descriptions and expert reviews
- Seller information (name, rating)
- Warranty information
- Pros/cons lists
- Review summaries / aggregate ratings (the overall score shifts slowly)
- Recommendation lists (model outputs refresh periodically)
- Best-selling product lists
- Brand landing page content
- Category landing page content (subcategories, popular brands)
- Digipay credit terms
- Editorial / magazine content

### 6.3 Short-Term Cache (5min)

Data that is volatile or time-sensitive:

- Product prices (selling price, RRP, discount)
- Stock / availability status
- Individual review/comment pages
- Current rating values
- Incredible offers / lightning deals (time-bound)
- Autocomplete suggestions
- Seller vouchers (may expire)
- Tag search results
- Q&A pages (new questions appear frequently)
- Product variant availability and pricing

### 6.4 No Cache

- `GET /v1/user/init/` — session-scoped
- `GET /fresh/v1/init/` — session-scoped
- `GET /api/features/{sdk_key}` — runtime feature flags
- `POST /api/3/envelope/` — Sentry telemetry (not called)
- `GET /v1/comments-overview/{id}/user-feedback/` — user-specific state

---

## 7. Upstream API Conventions

Extracted from the observed traffic:

### 7.1 Common Response Envelope

All Digikala API endpoints return:

```json
{
  "status": 200,
  "data": { ... }
}
```

The `status` field is an application-level status code (mirrors HTTP status). The `data` field contains the actual payload.

### 7.2 Pagination

- **Page-based** (`?page=N`): Used by search, comments, questions, tags, incredible-offers/products, rate-review, providers-products. Response includes a `pager` object.
- **Offset-based** (`?offset=N`): Used by product recommendation endpoints.

### 7.3 Sorting

Observed sort values: `1` (relevance), `25` (best-selling), `27` (newest), `created_at` (for Q&A). Endpoints return a `sort_options` array describing available options.

### 7.4 Default Headers (Required)

| Header | Value |
|---|---|
| `X-Web-Optimize-Response` | `1` |
| `X-Web-Client` | `desktop` |
| `Origin` | `https://www.digikala.com` |
| `Referer` | `https://www.digikala.com/` |

### 7.5 Fresh vs Main

Some endpoints have a `/fresh/v1/` prefix — these are the grocery/supermarket ("Digikala Fresh") variants. They accept a `_whid` (warehouse ID) query parameter. The MCP server should support both main and Fresh product views.

---

## 8. Excluded Endpoints

The following are present in the API surface but are **not exposed as MCP tools** (they serve internal/frontend purposes only):

| Endpoint | Reason for Exclusion |
|---|---|
| `POST /api/3/envelope/` | Sentry telemetry — no value for AI |
| `GET /v1/product/{id}/cpc/` | Sponsored ad placements — no value for AI |
| `GET /v1/product/{id}/pindo/` | Classified ad links — no value for AI |
| `GET /v1/magnet/home-page/` | Editorial content — low priority for product search |
| `GET /v1/magnet/notification-center/announcement/message/` | UI notification — no value for AI |
| `GET /v1/magnet/related-posts/{id}/` | Editorial posts — low priority |
| `GET /v1/dynamic-landing/mobile/` | Mobile landing layout — no value for AI |
| `GET /v1/dynamic-landing/widget/{id}/` | Widget data — no value for AI |
| `GET /v1/providers-products/` | Provider search — observed empty, unclear use |
| `GET /v1/comments-overview/{id}/user-feedback/` | User-specific state — not useful in anonymous context |
| `GET /api/features/{sdk_key}` | Feature flags — internal frontend config |

These endpoints are still fully modeled in the Infrastructure layer for completeness.

---

## 9. Development Guidelines

- **Target framework:** `net10.0`
- **Nullable reference types:** enabled project-wide
- **JSON serialization:** `System.Text.Json` with source generators for all request/response models
- **Code style:** follow existing repo conventions; file-scoped namespaces; primary constructors where appropriate
- **Testing:** unit tests for mapping logic and cache behavior; integration tests can mock `HttpMessageHandler`
- **NuGet packages:**
  - `ModelContextProtocol` 1.1.0
  - `Microsoft.Data.Sqlite` (for SQLite cache)
  - `Microsoft.Extensions.Http.Resilience` (Polly-based resilience)
  - `Microsoft.Extensions.Logging` / `Serilog` (logging)
  - `System.Text.Json` (serialization — included in SDK)

---

## 10. Milestones

| Phase | Scope |
|---|---|
| **Phase 1** | Solution scaffold, Domain entities, Infrastructure HTTP client for core endpoints (product, search, category), SQLite cache |
| **Phase 2** | Application layer DTOs, mapping, MCP tool handlers for T-01 through T-06 |
| **Phase 3** | Remaining tools (T-07 through T-15), full endpoint coverage in Infrastructure |
| **Phase 4** | Resilience policies, configuration, logging, tests |
| **Phase 5** | Polish, documentation, deployment config (Docker, systemd) |
