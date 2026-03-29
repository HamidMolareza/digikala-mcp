---
name: digikala-mcp-maintainer
description: Maintain the Digikala MCP .NET server in `/home/home/Desktop/temp/digikala/digikala-mcp`. Use when working on Digikala product/search MCP features, server configuration, logging, cache/database paths, tests, or docs-driven implementation against `docs/app-requirements.md` and `docs/request-response.api-summary.yaml`.
---

# Digikala MCP Maintainer

## Start Here

- Read `docs/app-requirements.md` and `docs/request-response.api-summary.yaml` before changing behavior.
- Keep MCP transport and `[McpServerTool]` wrappers in `src/Digikala.Mcp.Server/Tools`.
- Keep reusable use-case logic in `src/Digikala.Mcp.Application/Services`.
- Keep HTTP, caching, request/response models, and configuration in `src/Digikala.Mcp.Infrastructure`.

## Key Paths

- Server host: `src/Digikala.Mcp.Server`
- MCP wrappers: `src/Digikala.Mcp.Server/Tools/DigikalaTools.cs`
- Application services: `src/Digikala.Mcp.Application/Services`
- Mappers and DTOs: `src/Digikala.Mcp.Application/Mapping`, `src/Digikala.Mcp.Application/Dtos`
- Infrastructure client/cache: `src/Digikala.Mcp.Infrastructure/HttpClients`, `src/Digikala.Mcp.Infrastructure/Cache`
- Runtime data: `app-data/cache.db`, `app-data/logs/`
- Codex config entry: `/home/home/.gapcodex/config.toml`

## Workflow

1. Inspect the relevant requirement section in `docs/`.
2. Prefer small, layered changes that preserve the Domain → Application → Infrastructure → Server split.
3. When adding MCP behavior, keep protocol-specific code in Server and call Application services.
4. When adding upstream coverage, extend `IDigikalaApiClient`, `DigikalaHttpClient`, and request/response models together.
5. Update or add tests for mappers, services, cache behavior, and HTTP client behavior.

## Validation

- Restore/build: `dotnet restore Digikala.Mcp.slnx && dotnet build Digikala.Mcp.slnx --no-restore`
- Tests: `dotnet test Digikala.Mcp.slnx --no-build`
- Startup smoke: `timeout 5s dotnet run --project src/Digikala.Mcp.Server --no-build`

## Current Project Conventions

- Use `net10.0`, nullable enabled, file-scoped namespaces, and focused changes.
- Keep default runtime state under `app-data/`.
- Configure logging with Serilog and keep audit/error log paths configurable.
- Prefer compact AI DTOs in Application; never expose raw upstream JSON directly from MCP tools.
