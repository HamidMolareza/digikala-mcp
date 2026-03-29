# AGENTS

## Purpose

Maintain the Digikala MCP server without breaking the layered architecture.

## Layering Rules

- Keep MCP attributes and protocol wrappers in `src/Digikala.Mcp.Server`.
- Keep reusable use-case logic in `src/Digikala.Mcp.Application/Services`.
- Keep DTOs and mapping in `src/Digikala.Mcp.Application`.
- Keep Digikala HTTP access, cache persistence, EF models, and configuration in `src/Digikala.Mcp.Infrastructure`.
- Keep abstractions in `src/Digikala.Mcp.Domain`.

## Project-Specific Rules

- Read `docs/app-requirements.md` and `docs/request-response.api-summary.yaml` before implementing new behavior.
- Prefer adding typed request/response coverage in Infrastructure even when a new endpoint is not exposed as an MCP tool.
- Use `app-data/` for runtime database and log outputs.
- Use Serilog for runtime logging.
- Use EF Core for local database persistence where practical.

## Validation

- Build: `dotnet build Digikala.Mcp.slnx --no-restore`
- Tests: `dotnet test Digikala.Mcp.slnx --no-build`
- Startup smoke: `timeout 5s dotnet run --project src/Digikala.Mcp.Server --no-build`

## Change Style

- Make focused changes.
- Add or update tests for mapper, service, cache, and client behavior.
- Keep MCP tools thin and move decision logic into services.
