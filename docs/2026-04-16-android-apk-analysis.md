# Digikala Android APK Analysis

Date: 2026-04-16
APK: `/home/home/my-files/projects/apk-files/Digikala.apk`

## Goal

Capture the Android app's observable backend surface and record what is useful for `digikala-mcp` when the server must stay on unauthenticated, read-only APIs.

## APK findings

- Package: `com.digikala`
- Version: `3.1.3-CB`
- Version code: `304`
- Stack signal: React Native (`assets/index.android.bundle` is present)
- The production mobile config embedded in the bundle points at:
  - `https://sirius.digikala.com/`
  - `https://ravenclaw.digikala.com/`
- The bundle also references:
  - `https://api.digikala.com`
  - `https://api-zakaria.digikala.com`
  - `https://sentry.digikala.com`

## Modular mobile delivery

The APK includes `assets/custom/bundle-manifest.json`, which shows that the mobile app is delivered as multiple remote JS bundles rather than one monolith.

Observed modules:

- `digikala`
- `magnet`
- `digiplus`
- `checkout`
- `ventures`
- `user`

Observed release bundle host:

- `https://digikala-shopping.digikala.com/<guid>`

Why this matters for `digikala-mcp`:

- `magnet`, `digiplus`, `checkout`, and `user` are clearly separate product areas in the app.
- This is useful as a product-surface map even if the MCP server continues to use only the public web API.
- It supports keeping the MCP server modular too: core shopping tools, content/editorial tools, and future optional surfaces should stay clearly separated.

Evidence excerpt from `assets/index.android.bundle`:

```text
"apiServerBaseUrl":{"production":"https://sirius.digikala.com/","demo":"https://demo-sirius.digikala.com/"}
"clientServerBaseUrl":"https://demo-ravenclaw.digikala.com"
```

## MCP implications

- Keep the MCP server focused on the public Digikala web API surface already documented in `docs/app-requirements.md` and `docs/request-response.api-summary.yaml`.
- Do not pivot the MCP server to `sirius` or `ravenclaw` based only on the APK bundle config. The APK proves those hosts exist, but this pass did not recover a stable anonymous mobile endpoint catalog from them.
- Keep `sentry.digikala.com` out of scope.
- Continue treating user/session-specific flows as out of scope even if some bootstrap endpoints can be hit without a login in browser captures.

## Deep links and route families

The Android manifest exposes a larger route contract than the current MCP tools.

Verified hosts and schemes:

- `https://digikala.com`
- `https://www.digikala.com`
- `https://mehr.digikala.com`
- `https://www.mehr.digikala.com`
- `digikala://digikala.com`
- `digikala://digikala`
- `digikala://test_device`

Verified route families:

- `/product/dkp-*`
- `/categories/*`
- `/search/*`
- `/main/*`
- `/profile/*`
- `/promotion-center/*`
- `/incredible-offers/*`
- `/fresh-offers/*`
- `/product-list/plp_*`
- `/product-page/plp_*`
- `/promotion-page/*`
- `/brand/*`
- `/seller/*`
- `/cart/*`
- `/magnet/*`
- `/login-register/*`
- `/payment/*`
- `/order/*`
- `/wishlist/*`
- `/users/*`
- `/personalized-category/*`
- `/early-access-offers/*`
- `/early-access-fresh/*`
- `/landing-page/*`
- `/promotion-search/*`
- `/checkout/*`
- `/fresh/*`
- `/gold/*`
- `/brand-landing/*`
- `/product/*`
- `/compare/dpk*`
- `/digiplus/*`
- `/dknow/*`
- `/superapp/*`

Why this matters for `digikala-mcp`:

- Some route families already align with the current typed client and requirements, especially `fresh`, `magnet`, `incredible-offers`, `brand`, and `personalized-category`.
- Some route families are not represented as MCP tools today and are worth future anonymous API investigation:
  - `seller`
  - `compare`
  - `brand-landing`
  - `promotion-search`
  - `landing-page`
  - `dknow`
  - `superapp`
  - `gold`
- These are route-level signals only. They are not enough by themselves to add new tools, but they are good discovery targets.

## Payment and out-of-scope surfaces

The manifest also exposes a Digipay callback activity:

- `dgp-sdk://mydigipay.com/ipg-result`

This confirms the Android app integrates Digipay payment flows, but it stays out of scope for `digikala-mcp`.

Other clear non-MCP surfaces in the APK:

- Firebase Messaging / Performance / Remote Config
- InTrack push and in-app messaging
- Adjust / AdTrace install attribution
- Sentry / Crashlytics telemetry

These are implementation details of the mobile app and should not be exposed as MCP capabilities.

## Current repo alignment

The current typed client already covers the main anonymous read-only surface needed by the MCP server:

- session/bootstrap: `/v1/user/init/`, `/fresh/v1/init/`, `/v2/`
- feature flags: `/api/features/{sdk_key}`
- product detail and sub-resources
- category search and Fresh category search
- tags, incredible offers, best-selling, autocomplete, brand premium
- recommendation, dynamic landing, magnet/content, comments overview

That means the APK analysis did not reveal a missing must-have anonymous endpoint for the current MCP scope. The main value of this pass is to document host boundaries and avoid drifting toward mobile-internal services.

## Live probe result from this machine

On 2026-04-16, the following anonymous probes from this environment returned `403` HTML responses, even with browser-like headers:

- `https://api.digikala.com/v1/user/init/`
- `https://api.digikala.com/v1/autocomplete/?q=...`
- `https://api.digikala.com/v1/categories/.../search/`
- `https://api.digikala.com/v2/product/.../`
- `https://api-zakaria.digikala.com/api/features/...`
- `https://sirius.digikala.com/`

Interpretation:

- This looks like edge/WAF/geography/network gating, not evidence that the endpoints require user authentication.
- Live contract tests should therefore be run only from an egress path that Digikala allows.

## Recommended project rule

For `digikala-mcp`, prefer this decision rule:

1. Use only anonymous, read-only endpoints already validated in repo docs or fixtures.
2. Do not add endpoints that need login, cart state, profile state, orders, payment, or Digipay.
3. Treat manifest deep-link families as discovery hints, not implementation proof.
4. Do not adopt `sirius`/`ravenclaw` without a separate capture showing stable anonymous routes and response shapes.
