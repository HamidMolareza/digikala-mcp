# Digikala MCP

An MCP (Model Context Protocol) server that exposes Digikala's product APIs as AI-callable tools, enabling LLMs to search, browse, compare, and inspect products.

## Architecture

```
Domain          → Entities, value objects, enums, interfaces (zero dependencies)
Application     → AI DTOs, MCP tool handlers, mapping logic
Infrastructure  → HTTP client (all 40 endpoints), response models, SQLite cache
Server          → MCP host, DI composition root
```

## Requirements

- .NET 10 SDK
- `ModelContextProtocol` v1.1.0

## Quick Start

```bash
cd src/Digikala.Mcp.Server
dotnet run
```

The server communicates over **stdio** using the MCP protocol.

## Configuration

Edit `src/Digikala.Mcp.Server/appsettings.json` or use environment variables:

| Key | Default | Description |
|---|---|---|
| `Digikala:BaseUrl` | `https://api.digikala.com` | Primary API |
| `Cache:DatabasePath` | `./cache.db` | SQLite cache file |
| `Cache:ShortTermTtlMinutes` | `5` | Volatile data TTL |
| `Cache:MediumTermTtlMinutes` | `60` | Moderate-change data TTL |
| `Cache:LongTermTtlMinutes` | `1440` | Stable data TTL |

## Available MCP Tools

| Tool | Description |
|---|---|
| `search_products` | Search by keyword/category with filters and pagination |
| `get_product_detail` | Full product info (price, specs, variants, seller) |
| `get_product_price_chart` | Historical price data |
| `get_product_reviews` | Paginated user reviews |
| `get_product_questions` | Paginated Q&A |
| `get_product_recommendations` | Similar/recommended products |
| `get_best_selling` | Best sellers by category/time window |
| `get_incredible_offers` | Lightning deals and offers |
| `get_trending_products` | Trending products |
| `get_category_landing` | Category page with subcategories and brands |
| `browse_by_tag` | Products by SEO tag |
| `autocomplete` | Search suggestions and trends |
| `get_brand_page` | Premium brand landing page |
| `get_seller_vouchers` | Seller promotions for a product |
| `get_digipay_credit` | Installment/credit options |

## Tests

```bash
dotnet test
```
