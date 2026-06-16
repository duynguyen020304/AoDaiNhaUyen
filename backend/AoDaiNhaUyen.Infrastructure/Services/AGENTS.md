<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# Services

## Purpose
Infrastructure service implementations. These coordinate repositories, EF Core, external APIs, S3, cache, email, AI, and admin workflows.

## Service Groups
| Group | Main files |
|-------|------------|
| Auth/security | `AuthService`, `JwtTokenService`, `RefreshTokenService`, `Pbkdf2PasswordHasher`, `GoogleOAuthService`, `FacebookOAuthService`, `ZaloOAuthService` |
| Customer commerce | `CartService`, `CheckoutService`, `OrderService`, `StockService`, `PromoService`, `PromoCostService` |
| Catalog/content | `CatalogService`, `BlogPostService`, `BlogCategoryService`, `CommentService`, `AdminReviewService` |
| AI stylist/try-on | `StylistChatService`, `CatalogStylingService`, `CatalogTryOnService`, `VertexAiTryOnService`, `VertexAiStylistResponseComposer`, `IntentClassifier`, `ThreadMemoryService`, `StylistFallbackTextService` |
| AI safety/audit | `SafetyGate`, `LlmAuditService`, `PromptRedactionService`, `ConcurrencyLimitedAiTryOnService`, `ConcurrencyLimitedStylistChatService`, `VertexAiImageValidationService`, `CachedImageValidationService` |
| Admin AI/Hermes | `AdminAgentService`, `HermesAgentService`, `AdminChatPersistence`, `AutoModeStore`, `PendingActionStore`, `AdminToolRiskService`, `VertexAiAdminProvider` |
| Storage/media | `S3StorageService`, `UploadStoragePathResolver`, `ImageUploadValidator`, `ImageVisibilityService`, `BlogImageVisibilityService` |
| Email/marketing | `EmailTemplateService`, `EmailQueueService`, `BackgroundEmailWorker`, `SubscriberService`, `MarketingConsentService`, `CustomerEventService`, `OrderAttributionService` |
| Admin CRUD/dashboard | `AdminProductService`, `AdminCategoryService`, `AdminOrderService`, `AdminUserService`, `AdminRoleService`, `AdminMediaService`, `AdminDashboardService`, `AdminInventoryService`, `AdminPromoService`, `AdminMarketingServices` |
| Cache | `FusionCacheService`, `CacheKeyService`, `CacheInvalidationService` |
| Text/utils | `ChatTextUtils`, `BlogAiDraftService` |

## Hot Paths
- `StylistChatService` is main chat orchestrator: thread lifecycle -> intent -> catalog/try-on/tool calls -> persistence -> Gemini composer/fallback.
- `CatalogTryOnService` builds catalog assets and delegates image generation.
- `AuthService` owns register/login/OAuth/session/cookie token flow; controllers only set/clear cookies.
- `EmailQueueService` + `BackgroundEmailWorker` handle async marketing/template email sends.
- `HermesAgentService` and admin AI services power admin control-plane/chat actions.

## Local Conventions
- Primary constructor DI common.
- Return result objects instead of throwing for expected business failures.
- Vietnamese text processing lives in `IntentClassifier`, `ChatTextUtils`, fallback/composer services.
- Use storage/cache abstractions instead of direct S3/Redis/URL string logic in callers.
- Register all concrete services in `Api/Configuration/ServiceRegistration.cs`.
