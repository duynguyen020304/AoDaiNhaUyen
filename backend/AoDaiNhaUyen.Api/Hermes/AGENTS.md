<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Hermes

## Purpose
API description metadata layer for the Hermes autonomous admin agent. Defines strongly-typed record models that describe every admin endpoint (method, route, path/query params, request body schema, response shape, agent notes) and a registry that maps an incoming HTTP method + path to the matching description. Used by `HermesApiDescriptionMiddleware` to serve self-describing endpoint docs when Hermes sends `X-Hermes-Describe: true`.

## Key Files
| File | Description |
|------|-------------|
| `HermesAdminApiDescription.cs` | Sealed record types: `HermesAdminApiDescription`, `HermesParamDescription`, `HermesFieldDescription`, `HermesBodyDescription`, `HermesResponseDescription`. Pure data shapes — no logic. |
| `HermesAdminApiDescriptionRegistry.cs` | Singleton-safe registry. Builds a static list of `HermesAdminApiDescription` records covering all admin routes (dashboard, products, categories, users, orders, inventory, LLM logs, media, promos, roles, tool-risk, marketing, Hermes reports/events, AI chat). `Find(method, path)` does route-template regex matching. `KnownRoutes()` returns all registered `METHOD /route` strings for 404 hints. |

## For AI Agents

### Working In This Directory
- When adding a new admin endpoint, add a matching entry in `HermesAdminApiDescriptionRegistry.BuildDescriptions()`.
- Route templates use `{paramName}` placeholders — the registry converts them to `[^/]+` regex segments for matching.
- Notes arrays should be written in Vietnamese (consistent with existing entries).
- Never store secrets or user PII in the description records — they are returned verbatim to the Hermes runner.
- `HermesResponseDescription` uses a shared `Envelope` constant; override only `DataShape` per endpoint via `with { DataShape = "..." }`.

### Common Patterns
- Factory helpers `Get/Post/Put/Patch/Delete` delegate to private `Desc(...)`.
- `Body(fields, example)` for JSON bodies; `MultipartBody(fieldName, desc)` for file uploads.
- `Param(name, type, required, description)` for path/query params; `Id(name, desc)` shorthand for required GUID params.

## Dependencies
### Internal
- `AoDaiNhaUyen.Api.Middleware.HermesApiDescriptionMiddleware` — consumes this registry
- `AoDaiNhaUyen.Api.Authentication.HermesAdminAuthOptions` — scheme used for description auth check
### External
- None (pure BCL)

<!-- MANUAL: -->
