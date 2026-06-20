<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-06-19 -->

# AoDaiNhaUyen.Api

## Purpose
ASP.NET Core 10 API host. Owns `Program.cs`, HTTP controllers, API response envelope, middleware, auth/CORS/static-file pipeline, DI registration, `.env`/appsettings config.

## Key Files
| File | Description |
|------|-------------|
| `Program.cs` | Entry point: `.env`, CORS, middleware order, static files, auth, rate limiter, controllers, seed-on-startup |
| `Configuration/ServiceRegistration.cs` | `AddBackendServices()`: DbContext, auth, options, repos, services, HttpClients |
| `Responses/ApiResponseFactory.cs` | Standard success/failure/pagination response helpers |
| `AoDaiNhaUyen.Api.http` | Basic HTTP smoke file |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Authentication/` | Hermes API key auth scheme and options (see `Authentication/AGENTS.md`) |
| `Configuration/` | `AddBackendServices()` DI wiring for all services (see `Configuration/AGENTS.md`) |
| `Controllers/` | Customer + admin HTTP endpoints (see `Controllers/AGENTS.md`) |
| `Hermes/` | API description records and registry for Hermes self-describe feature (see `Hermes/AGENTS.md`) |
| `Middleware/` | Exception handling, cache headers, Hermes API description (see `Middleware/AGENTS.md`) |
| `Responses/` | API envelope records/factory/errors (see `Responses/AGENTS.md`) |
| `Services/` | API-host services such as SMTP email (see `Services/AGENTS.md`) |
| `HttpTests/` | `.http` endpoint test files |
| `upload/` | Legacy/static local assets; curated try-on files still resolved here |

## Controller Groups
| Group | Controllers |
|-------|-------------|
| Customer catalog/content | `ProductsController`, `CategoriesController`, `BlogPostsController`, `MediaController` |
| Customer commerce | `UserCartController`, `CheckoutController`, `UserOrderController`, `PromoController` |
| Customer auth/account | `AuthController`, `UserController`, `UserAddressController` |
| Customer AI/feedback | `AiTryOnController`, `ChatController`, `ReviewsController`, `CommentsController` |
| Marketing/events | `MarketingController`, `EventsController` |
| Admin CRUD/ops | `AdminProductsController`, `AdminCategoriesController`, `AdminOrdersController`, `AdminUsersController`, `AdminRolesController`, `AdminMediaController`, `AdminInventoryController`, `AdminPromosController` |
| Admin AI/audit | `AdminAiController`, `AdminHermesController`, `AdminLlmLogsController`, `AdminToolRiskController`, `AdminDashboardController`, `AdminMarketingController` |
| Infra | `HealthController`, `CacheController` |

## Middleware
| File | Description |
|------|-------------|
| `ExceptionHandlingMiddleware.cs` | Logs unhandled exceptions and returns Vietnamese 500 envelope |
| `SensitiveResponseCacheMiddleware.cs` | Adds cache-control protection for sensitive responses |
| `HermesApiDescriptionMiddleware.cs` | Exposes API description metadata for Hermes/admin agent integration |

## Pipeline Notes
- Order in `Program.cs`: CORS -> exception middleware -> prod HSTS/HTTPS -> security headers -> static files -> sensitive-cache middleware -> auth -> rate limiter -> Hermes description -> authorization -> controllers.
- CORS policy `Frontend` uses `FrontendOrigins`, allows credentials, any header, any method.
- JWT bearer auth extracts token from HttpOnly cookie, not only `Authorization` header.
- Static files expose local `upload/`; app also supports S3-compatible storage for uploaded media.

## Local Conventions
- Controllers use primary constructor DI and stay thin.
- Use `ApiResponseFactory.Success/Failure/PaginatedSuccess`; avoid raw anonymous objects.
- New services/repos must be registered in `ServiceRegistration.cs`.
- Auth cookies: access cookie path `/`; refresh cookie path `/api/auth`.
- Image upload endpoints validate content type/size; GIF rejected for AI image flow.
