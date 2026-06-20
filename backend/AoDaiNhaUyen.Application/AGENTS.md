<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# AoDaiNhaUyen.Application

## Purpose
Application layer defining contracts, data shapes, and lightweight services for the Ao Dai e-commerce platform. Contains DTOs, service and repository interfaces, strongly-typed option classes, domain exceptions, and application-level services (catalog, blog, cache management). Infrastructure implements all service and repository interfaces; the API layer consumes them. No EF Core, no HTTP concerns here.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Constants/` | Shared string constants (cache tags) used across Application and Infrastructure |
| `DTOs/` | Request/response record types for all domain areas |
| `Exceptions/` | Domain-level custom exceptions thrown by services |
| `Interfaces/` | Service and repository contracts implemented in Infrastructure |
| `Options/` | Strongly-typed configuration classes bound from appsettings |
| `Services/` | Application-layer service implementations (catalog, blog, comment, cache) |

## DTO Map
| Area | Location |
|------|----------|
| Catalog, AI try-on, chat, paging | `DTOs/` root — product/category/chat/try-on/memory/review DTOs |
| Admin | `DTOs/Admin/` — products, promos, users, Hermes agent, AI, media, inventory, LLM audit |
| Auth | `DTOs/Auth/` — sessions, JWT results, OAuth user info, token validation |
| Blog | `DTOs/BlogPost/` — list/detail/create/update/blocks/images |
| Cart | `DTOs/Cart/` — add, update, full cart view |
| Checkout | `DTOs/Checkout/` — address, request, result |
| Dashboard | `DTOs/Dashboard/` — admin analytics charts and stats |
| Marketing | `DTOs/Marketing/` — subscriber, event tracking, promo performance |
| Order | `DTOs/Order/` — order status updates, shipment |
| Promo | `DTOs/Promo/` — discount validation result |
| User | `DTOs/User/` — profile, addresses, order history |

## Interfaces Map
| Area | Location |
|------|----------|
| Cache primitives | Root `Interfaces/` — `IFusionCacheService`, `ICacheInvalidationService`, `ICacheKeyService` |
| Seeding | Root `Interfaces/` — `ISeedDataService` |
| Repositories | `Interfaces/Repositories/` — Category, Product, Cart, UserProfile, BlogCategory, BlogPost, Comment |
| All services | `Interfaces/Services/` — ~60 service contracts covering auth, catalog, cart, checkout, blog, chat, AI try-on, admin, marketing, email, storage |

## Options
| Class | Section | Purpose |
|-------|---------|---------|
| `JwtSettings` | `Jwt` | Access/refresh token lifetimes, signing key |
| `CookieSettings` | _(direct)_ | Cookie names for access/refresh tokens |
| `EmailSettings` | `Email` | SMTP host/port/credentials and base URLs |
| `GoogleOAuthSettings` | `GoogleOAuth` | Client ID/secret/redirect for Google login |
| `ZaloOAuthSettings` | `ZaloOAuth` | App ID/secret/redirect for Zalo login |
| `AdminSeedOptions` | `AdminSeed` | Bootstrap admin email/password |
| `AiTryOnConcurrencyOptions` | `AiTryOnConcurrency` | Max concurrent Vertex AI try-on calls |
| `ChatConcurrencyOptions` | `ChatConcurrency` | Max concurrent stylist chat threads |
| `HermesAgentOptions` | `Hermes` | Admin Hermes agent API server URL/key/runner name |
| `HermesOutboxOptions` | `HermesOutbox` | Outbox worker poll interval, batch size, thresholds |
| `ImageValidationOptions` | `ImageValidation` | Max size, dimension limits, allowed extensions |

## For AI Agents

### Working In This Directory
- Do not reference `AoDaiNhaUyen.Infrastructure` from this project — dependency flows inward only
- DTOs are `sealed record` types; use positional syntax for simple records, init-property syntax when validation attributes are needed
- New features: define interface here, implement in Infrastructure, register in `ServiceRegistration.cs`
- Options classes use `DataAnnotations` and are validated on startup via `AddOptions<T>().ValidateDataAnnotations()`
- Result types follow the pattern `{ Succeeded, Value, ErrorCode, ErrorMessage }` (see `AdminMutationResult`, `AuthResult`)

### Common Patterns
- `PagedResult<T>` wraps all paged responses: `Items`, `TotalCount`, `Page`, `PageSize`
- Cache tags in `CacheTags` constants are used to invalidate groups of entries via `ICacheInvalidationService`
- `IFusionCacheService.GetOrSetAsync()` with tag arrays is the standard cache-aside call in services
- Async methods accept `CancellationToken` as last parameter with a default value of `default`

## Dependencies
### Internal
- `AoDaiNhaUyen.Domain` — entities and enums used in repository return types

### External
- `System.ComponentModel.DataAnnotations` — option/request validation attributes

<!-- MANUAL: -->
