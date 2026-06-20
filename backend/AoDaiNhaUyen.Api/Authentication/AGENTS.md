<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Authentication

## Purpose
Hermes-specific API key authentication scheme. Provides a custom ASP.NET Core `AuthenticationHandler` that validates the `X-Hermes-Admin-Key` header using constant-time comparison, then synthesises an Admin-role `ClaimsPrincipal` for the Hermes runner identity. Used alongside JWT bearer auth so Hermes can call admin endpoints without a human user session.

## Key Files
| File | Description |
|------|-------------|
| `HermesAdminAuthOptions.cs` | Options POCO holding `AdminApiKey`; also declares the scheme name (`HermesAdminKey`) and header name (`X-Hermes-Admin-Key`). Bound from `appsettings` section `Hermes`. |
| `HermesAdminApiKeyAuthenticationHandler.cs` | `AuthenticationHandler` implementation. Reads the header, does a `CryptographicOperations.FixedTimeEquals` check, emits a fixed synthetic user ID (`00000000-0000-0000-0000-000000000001`) with `hermes_agent` name and `Admin` role. Returns `NoResult` (not `Fail`) when the header is absent so JWT can still proceed. |

## For AI Agents

### Working In This Directory
- Do not add a second authentication scheme here; extend `ServiceRegistration.cs` if a new scheme is needed.
- The synthetic Hermes user ID is a sentinel constant — never reuse it for real users.
- Constant-time comparison is mandatory for any key comparison; do not replace with `==`.

### Common Patterns
- `AuthenticateResult.NoResult()` when header is absent (allows JWT fallback).
- `AuthenticateResult.Fail(...)` when header is present but key is wrong.
- Claims emitted: `NameIdentifier`, `Name`, `Role` (Admin), and `agent` = `"hermes"`.

## Dependencies
### Internal
- `AoDaiNhaUyen.Domain.Constants.RoleNames` — role name constant
- `AoDaiNhaUyen.Api.Configuration.ServiceRegistration` — registers this scheme
### External
- `Microsoft.AspNetCore.Authentication`
- `System.Security.Cryptography.CryptographicOperations`

<!-- MANUAL: -->
