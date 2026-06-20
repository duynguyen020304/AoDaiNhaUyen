<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# backend

## Purpose
ASP.NET Core 10 REST API with clean architecture. Handles catalog, cart/checkout, auth (credentials + Google/Facebook/Zalo OAuth), AI try-on/chat, Blog CMS, reviews/comments, promos, email marketing, admin dashboards, Hermes admin agent with outbox worker, media/S3, and audit/risk controls.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.slnx` | Solution file for Api/Application/Domain/Infrastructure (excludes Tests) |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AoDaiNhaUyen.Api/` | Web API host: Program.cs, DI, controllers, middleware, responses |
| `AoDaiNhaUyen.Application/` | DTOs, interfaces, options, app services |
| `AoDaiNhaUyen.Domain/` | Entities + seed data; no external deps |
| `AoDaiNhaUyen.Infrastructure/` | EF Core, repos, external integrations, most services |
| `AoDaiNhaUyen.Tests/` | xUnit tests; not included in `.slnx` (see `AoDaiNhaUyen.Tests/AGENTS.md`) |

## For AI Agents

### Working In This Directory

### Code Map
| Need | File/dir |
|------|----------|
| Routes | `AoDaiNhaUyen.Api/Controllers/` |
| DI registrations | `AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs` |
| Middleware order | `AoDaiNhaUyen.Api/Program.cs` |
| Entity mappings | `AoDaiNhaUyen.Infrastructure/Data/AppDbContext.cs` |
| Seed data | `AoDaiNhaUyen.Domain/SeedData/`, `AoDaiNhaUyen.Infrastructure/Data/SeedDataService.cs` |
| Service contracts | `AoDaiNhaUyen.Application/Interfaces/Services/` |
| Repository contracts | `AoDaiNhaUyen.Application/Interfaces/Repositories/` |
| Service impls | `AoDaiNhaUyen.Infrastructure/Services/` |
| Migrations | `AoDaiNhaUyen.Infrastructure/Data/Migrations/` |

### Common Patterns
- Dependency flow: Api -> Application + Infrastructure; Infrastructure -> Application + Domain; Application -> Domain; Domain standalone.
- Controllers return `ApiResponse<T>` / `PaginatedApiResponse<T>` via `ApiResponseFactory`.
- PostgreSQL table/column names use snake_case via `AppDbContext`.
- Auth uses HttpOnly cookies: access token + refresh token; JWT bearer reads cookie in auth event.
- Config options use Options pattern + validation: JWT, cookies, email, OAuth, GoogleCloud, S3, Hermes, FusionCache.
- Repositories: catalog/cart/user profile/blog/comment data access only. Business logic lives in services.

## Commands
| Task | Command |
|------|---------|
| Build | `dotnet build` from `backend/` |
| Tests | `dotnet test` from `backend/AoDaiNhaUyen.Tests/` |
| Migration | `cd AoDaiNhaUyen.Infrastructure && dotnet ef migrations add <Name> --startup-project ../AoDaiNhaUyen.Api` |

## Dependencies
### Internal
- References own projects only: Api -> Application + Infrastructure; no cross-cutting to frontend.

### External
- ASP.NET Core 10, EF Core 10, Npgsql/PostgreSQL
- JWT Bearer, MailKit, DotNetEnv
- FusionCache + optional Redis L2/backplane
- AWS S3-compatible storage
- Google Vertex/Gemini for try-on, image validation, stylist/admin AI

## Gotchas
- `.slnx` omits `AoDaiNhaUyen.Tests`; `dotnet test` from `backend/` may skip tests — run from `backend/AoDaiNhaUyen.Tests/` explicitly.
- `RunMigrationsAndSeedOnStartup` can migrate/seed DB on app start.
- S3-compatible storage is primary for uploads/media; older `upload/` paths still exist for static/curated assets.
- FusionCache may run L1-only if Redis is unavailable/config missing.
- CORS policy uses configured frontend origins + credentials + allow-any header/method.
- Hermes outbox worker polls DB for pending events; configure `HermesOutbox__*` env vars in deployment.
