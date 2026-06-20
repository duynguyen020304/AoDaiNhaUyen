<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Middleware

## Purpose
Custom ASP.NET Core middleware components that run in the request pipeline. Covers global exception handling, cache-control header injection for sensitive routes, and the Hermes API self-description intercept layer. All three are registered in `Program.cs` in a defined order (exception handler first, then sensitive-cache, then Hermes description after auth).

## Key Files
| File | Description |
|------|-------------|
| `ExceptionHandlingMiddleware.cs` | Catches any unhandled exception, logs it, and writes a Vietnamese `ApiResponseFactory.Failure` 500 JSON envelope. Skips writing if the response has already started. |
| `SensitiveResponseCacheMiddleware.cs` | Injects `Cache-Control: private, no-store, no-cache, must-revalidate` + `Pragma: no-cache` + `Expires: 0` for any path under `/api/admin`, `/api/auth`, `/api/users`, `/api/user`, `/api/cart`, `/api/checkout`, or `/api/promo`. |
| `HermesApiDescriptionMiddleware.cs` | Intercepts `X-Hermes-Describe: true` requests under `/api/admin/*`. Validates the `HermesAdminKey` auth scheme, looks up the matching `HermesAdminApiDescription` from the registry, and returns it as JSON — short-circuiting the actual controller. Returns 401 or 404 if auth fails or the route is not catalogued. |

## For AI Agents

### Working In This Directory
- Middleware ordering in `Program.cs` is intentional — do not reorder without checking dependencies.
- `ExceptionHandlingMiddleware` must be outermost to catch errors from all subsequent middleware.
- `HermesApiDescriptionMiddleware` runs after auth middleware so `context.AuthenticateAsync` can resolve the Hermes scheme.
- When adding a new sensitive route prefix, add it to the `SensitiveApiPrefixes` array in `SensitiveResponseCacheMiddleware`.

### Common Patterns
- `InvokeAsync(HttpContext context)` + constructor injection via primary constructor.
- Short-circuit by returning early (no `next(context)` call) when the middleware handles the response itself.
- Use `context.Response.OnStarting(...)` to inject headers without buffering the body (as in `SensitiveResponseCacheMiddleware`).

## Dependencies
### Internal
- `AoDaiNhaUyen.Api.Responses.ApiResponseFactory` — builds error envelopes
- `AoDaiNhaUyen.Api.Hermes.HermesAdminApiDescriptionRegistry` — route lookup (injected via `InvokeAsync` parameter)
- `AoDaiNhaUyen.Api.Authentication.HermesAdminAuthOptions` — scheme name constant
### External
- `Microsoft.AspNetCore.Authentication` — `context.AuthenticateAsync`

<!-- MANUAL: -->
