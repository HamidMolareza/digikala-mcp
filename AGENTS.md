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
- Keep Digipay credit out of scope unless requirements explicitly change.
- Prefer adding typed request/response coverage in Infrastructure even when a new endpoint is not exposed as an MCP tool.
- Treat upstream Digikala response-shape drift as a first-class regression risk; add or update offline API contract fixtures/tests when a change touches an endpoint, mapper, or tool response path.
- Keep offline API contract fixtures under `tests/Fixtures/ApiContracts/` and keep the matching assertions in `tests/Digikala.Mcp.Application.Tests/OfflineApiContractTests.cs` or a similarly focused test file.
- Extend env-gated live contract tests in `tests/Digikala.Mcp.Infrastructure.Tests/LiveDigikalaContractTests.cs` when a change depends on a stable Digikala endpoint that should be probed against the real API.
- Use `app-data/` for runtime database and log outputs.
- Use Serilog for runtime logging.
- Use EF Core for local database persistence where practical.

## Validation

- Build: `dotnet build Digikala.Mcp.slnx --no-restore`
- Tests: `dotnet test Digikala.Mcp.slnx --no-build`
- Startup smoke: `timeout 5s dotnet run --project src/Digikala.Mcp.Server --no-build`
- If the local machine hits file-watcher or inotify limits during startup smoke, retry with `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false`.

## Change Style

- Make focused changes.
- Add or update tests for mapper, service, cache, and client behavior.
- For new upstream fields, endpoints, or mapping behavior, also add or refresh offline contract tests that prove the expected Digikala payload shape and mapped DTO output.
- Keep MCP tools thin and move decision logic into services.
- Keep temporary review notes and implementation checklists in `docs/` so the next implementation pass starts from repo-local context.
