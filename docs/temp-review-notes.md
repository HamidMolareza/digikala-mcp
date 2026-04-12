# Temporary Review Notes

Date: 2026-04-12

This file captures the current exploration findings so the next implementation pass can start from concrete repo-local notes.

## Highest-priority issues

- Some MCP tools still throw unhandled exceptions instead of returning structured `ToolIssueDto` responses.
- Error logs are noisy because transient Polly timeout/retry events are written at error level alongside real tool failures.
- `recommend_products_for_need` fans out into many upstream requests and is the riskiest workflow for latency and timeout regressions.

## Likely fixes

- Wrap direct service calls such as product detail, tag browse, autocomplete, and seller vouchers with the same structured error handling pattern already used in `SearchProductsService`.
- Reduce resilience log noise by separating transport/resilience events from actual tool failures or by lowering their sink level.
- Trim recommendation fan-out by reusing shared detail/review data, lowering candidate counts, or introducing category-specific recommendation flows.

## Useful feature ideas

- Expand category resolution for accessory shopping flows such as charger, case, and screen protector.
- Consider a bundle-oriented accessory recommendation workflow for phone purchases instead of routing everything through one generic need-based search.

## Scope decisions already made

- Digipay credit is removed from project scope.
- Old runtime logs should not be kept in the repo between implementation passes.
