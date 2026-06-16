<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# AoDaiNhaUyen.Infrastructure

## Purpose
Infrastructure layer: EF Core data access, migrations, repositories, external integrations, cache/storage, and most business service implementations.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Infra options such as Google Cloud |
| `Data/` | `AppDbContext`, migrations, seed service |
| `Repositories/` | EF Core repository implementations |
| `Services/` | Service implementations and external API adapters |

## Data Map
| File | Notes |
|------|-------|
| `Data/AppDbContext.cs` | DbSets + inline table/index/constraint/relationship config; snake_case names |
| `Data/AppDbContextFactory.cs` | EF CLI design-time factory |
| `Data/SeedDataService.cs` | Optional migrate+seed on startup |
| `Data/Migrations/` | EF migrations; timestamped names |

## Repository Map
| Area | Implementations |
|------|-----------------|
| Catalog/cart/user | `CategoryRepository`, `ProductRepository`, `CartRepository`, `UserProfileRepository` |
| Blog/comments | `BlogCategoryRepository`, `BlogPostRepository`, `CommentRepository` |

## Service Map
| Area | Examples |
|------|----------|
| Auth | `AuthService`, `JwtTokenService`, `RefreshTokenService`, `Pbkdf2PasswordHasher`, Google/Facebook/Zalo OAuth services |
| Commerce | `CartService`, `CheckoutService`, `OrderService`, `StockService`, `PromoService`, `PromoCostService` |
| Catalog/content | `CatalogService`, `BlogPostService`, `BlogAiDraftService`, `BlogImageVisibilityService` |
| AI chat/try-on | `StylistChatService`, `CatalogStylingService`, `CatalogTryOnService`, `VertexAiTryOnService`, `VertexAiStylistResponseComposer`, `IntentClassifier`, `ThreadMemoryService` |
| AI safety/admin | `SafetyGate`, `LlmAuditService`, `PromptRedactionService`, `AdminAgentService`, `HermesAgentService`, `VertexAiAdminProvider`, tool-risk services |
| Media/storage | `S3StorageService`, `UploadStoragePathResolver`, image visibility/validation services |
| Email/marketing | `EmailTemplateService`, `EmailQueueService`, `BackgroundEmailWorker`, `SubscriberService`, `MarketingConsentService`, `CustomerEventService`, `OrderAttributionService` |
| Admin CRUD | Admin product/category/order/user/role/media/dashboard/inventory/promo/marketing/review services |
| Cache | `FusionCacheService`, cache key/invalidation services |

## Local Conventions
- Services implement `Application/Interfaces/Services/*`; repos implement `Application/Interfaces/Repositories/*`.
- Register new implementations in `Api/Configuration/ServiceRegistration.cs`.
- EF config stays inline in `AppDbContext`; no separate configuration classes.
- External AI services are typed HttpClients; long-running image generation uses generous/infinite timeout.
- Redis/FusionCache should degrade gracefully to memory/L1 behavior where configured.
- Storage public URLs should flow through storage/path services, not string-built in controllers.
