# Implementation Checklist

Date: 2026-04-12

## Baseline cleanup

- [x] Remove Digipay credit from project scope, docs, and unused code paths.
- [x] Clear old runtime logs from `app-data/logs/`.
- [x] Save current review notes in `docs/`.

## Main implementation readiness

- [x] Normalize all tool-facing services to return structured errors instead of leaking exceptions.
- [x] Reduce error-log noise from resilience events so real MCP failures stay obvious.
- [x] Rework `recommend_products_for_need` to reduce upstream fan-out and timeout risk.
- [ ] Expand category resolution for accessory categories used in common shopping bundles.
- [ ] Add or refresh focused tests for any changed service, mapper, or error-handling path.
- [ ] Re-run build and tests before each handoff.

## Validation commands

- `dotnet build Digikala.Mcp.slnx --no-restore`
- `dotnet test Digikala.Mcp.slnx --no-build`
