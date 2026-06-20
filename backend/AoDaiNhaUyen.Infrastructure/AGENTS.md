<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# AoDaiNhaUyen.Infrastructure

## Purpose
Infrastructure layer: EF Core data access, database migrations, repository implementations, external service integrations (Google Vertex AI, S3/MinIO storage, email), cache (FusionCache/Redis), and the majority of application service implementations. Depends on Domain and Application; no reverse dependency.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Infrastructure.csproj` | Project file — EF Core, Npgsql, AWS SDK, FusionCache, Anthropic/Gemini client refs |
| `Configuration/GoogleCloudOptions.cs` | Strongly-typed options for Vertex AI (project, location, model names, timeouts) |
| `Configuration/S3StorageSettings.cs` | Options for S3-compatible storage (bucket, region, credentials, service URL) |
| `Data/AppDbContext.cs` | EF Core DbContext: all DbSets, snake_case mapping, PostgreSQL enums/inet/jsonb config |
| `Data/AppDbContextFactory.cs` | Design-time factory for `dotnet ef` CLI |
| `Data/SeedDataService.cs` | Startup migrate + upsert seeder |
| `Data/Migrations/` | 25 EF migrations (see `Migrations/AGENTS.md`) |
| `Repositories/` | EF Core repository implementations (see `Repositories/AGENTS.md`) |
| `Services/` | All service implementations (see `Services/AGENTS.md`) |

## Subdirectory Summary
| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Strongly-typed options POCOs for external services |
| `Data/` | DbContext, design-time factory, seed service, migrations |
| `Repositories/` | IRepository implementations wrapping AppDbContext |
| `Services/` | Business service implementations, external API adapters, background workers |

## Service Area Map
| Area | Key Services |
|------|-------------|
| Auth | `AuthService`, `JwtTokenService`, `RefreshTokenService`, `Pbkdf2PasswordHasher`, `GoogleOAuthService`, `ZaloOAuthService` |
| Commerce | `CartService`, `CheckoutService`, `OrderService`, `StockService`, `PromoService`, `PromoCostService` |
| Catalog/content | `AdminProductService`, `AdminCategoryService`, `BlogAiDraftService`, `BlogImageVisibilityService` |
| AI stylist/try-on | `StylistChatService`, `CatalogStylingService`, `CatalogTryOnService`, `VertexAiTryOnService`, `VertexAiStylistResponseComposer`, `IntentClassifier`, `ThreadMemoryService`, `StylistFallbackTextService` |
| AI safety/audit | `SafetyGate`, `LlmAuditService`, `PromptRedactionService`, `ConcurrencyLimitedAiTryOnService`, `ConcurrencyLimitedStylistChatService`, `VertexAiImageValidationService`, `CachedImageValidationService` |
| Admin AI/Hermes | `AdminAgentService`, `HermesAgentService`, `AdminChatPersistence`, `AutoModeStore`, `PendingActionStore`, `AdminToolRiskService`, `VertexAiAdminProvider`, `HermesEventOutboxPublisher`, `HermesEventProcessor`, `HermesFeedService`, `HermesMonitorLinkService`, `BackgroundHermesOutboxWorker` |
| Storage/media | `S3StorageService`, `UploadStoragePathResolver`, `ImageUploadValidator`, `ImageVisibilityService`, `BlogImageVisibilityService` |
| Email/marketing | `EmailTemplateService`, `EmailQueueService`, `BackgroundEmailWorker`, `SubscriberService`, `MarketingConsentService`, `CustomerEventService`, `OrderAttributionService` |
| Admin CRUD | `AdminOrderService`, `AdminUserService`, `AdminRoleService`, `AdminMediaService`, `AdminDashboardService`, `AdminInventoryService`, `AdminPromoService`, `AdminMarketingServices`, `AdminReviewService` |
| Cache | `FusionCacheService` |
| AI try-on feedback | `AiTryOnFeedbackService` |

## For AI Agents
### Working In This Directory
- Register all new service/repo implementations in `Api/Configuration/ServiceRegistration.cs` (not here).
- EF entity config stays inline in `AppDbContext.OnModelCreating` — no separate `IEntityTypeConfiguration<T>` classes.
- External AI services use typed `HttpClient`; long-running image generation uses generous/infinite timeouts.
- Storage public URLs must flow through `UploadStoragePathResolver`/`S3StorageService`, not string-built in controllers.
- Redis/FusionCache should degrade gracefully to memory/L1 behavior.

### Common Patterns
- Primary constructor DI (`public class Foo(AppDbContext db, ILogger<Foo> logger)`).
- Return result objects for expected business failures instead of throwing.
- Vietnamese text processing in `IntentClassifier`, `ChatTextUtils`, fallback/composer services.
- `AsNoTracking()` for read-only queries; tracked queries only when mutation follows.

## Dependencies
### Internal
- `AoDaiNhaUyen.Domain` (entities, enums, seed data)
- `AoDaiNhaUyen.Application` (service/repo interfaces)
### External
- EF Core + Npgsql provider, AWS SDK S3, FusionCache, StackExchange.Redis, Vertex AI / Google Cloud SDK

<!-- MANUAL: -->
