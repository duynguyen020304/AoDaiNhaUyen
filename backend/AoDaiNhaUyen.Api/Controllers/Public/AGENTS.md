<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Controllers/Public

## Purpose
Unauthenticated public endpoints. Currently contains the Hermes monitor controller, which exposes sanitized read-only snapshots of Hermes outbox events behind signed bearer URL tokens — allowing external stakeholders to observe event progress without an admin session.

## Key Files
| File | Route | Description |
|------|-------|-------------|
| `PublicHermesMonitorController.cs` | `api/public/hermes/monitor` | `GET /{token}` returns a sanitized `HermesMonitorSnapshotResponse` for the signed token. `GET /{token}/stream` streams snapshot updates via SSE every 5 seconds for up to 60 iterations, terminating early when the event reaches a terminal status (`completed`, `dead`, `cancelled`). Decorated `[AllowAnonymous]` and `[EnableRateLimiting("hermes-monitor")]`. |

## For AI Agents

### Working In This Directory
- This directory is for truly public (anonymous) endpoints only; do not add endpoints requiring auth here.
- Token validation and snapshot sanitisation are handled entirely by `IHermesMonitorLinkService` — the controller only routes the call.
- SSE streams terminate with `event: done\ndata: [DONE]\n\n`; max 60 polls × 5 s = 5 min cap.
- Rate limiter policy `"hermes-monitor"` is configured in `Program.cs`; confirm the policy exists before adding a new `[EnableRateLimiting]` attribute.

### Common Patterns
- `[AllowAnonymous]` at class level; no per-action auth overrides.
- `ApiResponseFactory.Failure(...)` with `"invalid_monitor_token"` code on 404.
- SSE event name convention: `"snapshot"` for data frames, `"completed"` for terminal, `"error"` for failures, `"done"` for stream end.

## Dependencies
### Internal
- `AoDaiNhaUyen.Application.Interfaces.Services.IHermesMonitorLinkService`
- `AoDaiNhaUyen.Api.Responses.ApiResponseFactory`
### External
- `Microsoft.AspNetCore.Authorization`
- `Microsoft.AspNetCore.RateLimiting`

<!-- MANUAL: -->
