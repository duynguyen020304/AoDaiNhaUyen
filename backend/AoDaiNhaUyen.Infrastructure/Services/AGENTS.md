<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Services

## Purpose
Infrastructure service implementations. These classes coordinate repositories, EF Core, external APIs (Vertex AI / Gemini, S3/MinIO, Google/Zalo OAuth), cache (FusionCache/Redis), email queuing, background workers, and admin AI/Hermes agent workflows. Implements interfaces from `Application/Interfaces/Services/`.

## Key Files
| File | Description |
|------|-------------|
| `AuthService.cs` | Register/login/OAuth/session/cookie token flow; owns full auth lifecycle |
| `JwtTokenService.cs` | JWT access token generation and validation |
| `RefreshTokenService.cs` | Refresh token issuance, rotation, and revocation |
| `Pbkdf2PasswordHasher.cs` | PBKDF2 password hashing and verification |
| `GoogleOAuthService.cs` | Google OAuth2 token exchange and profile fetch |
| `ZaloOAuthService.cs` | Zalo OAuth2 token exchange and profile fetch |
| `CartService.cs` | Cart read/add/update/remove; merges guest cart on login |
| `CheckoutService.cs` | Order creation from cart; stock reservation; promo application |
| `OrderService.cs` | Order status transitions, history, and detail retrieval |
| `StockService.cs` | Stock check and reservation for variants |
| `PromoService.cs` | Promo code validation, application, and usage tracking |
| `PromoCostService.cs` | Promo cost calculation and snapshot creation |
| `StylistChatService.cs` | Main AI stylist chat orchestrator: thread lifecycle → intent → catalog/try-on/tool calls → persistence → Gemini composer/fallback |
| `CatalogStylingService.cs` | Catalog-based style recommendations using product profiles |
| `CatalogTryOnService.cs` | Builds catalog assets and delegates image generation to Vertex AI |
| `VertexAiTryOnService.cs` | Vertex AI virtual try-on image generation via Google Cloud |
| `VertexAiStylistResponseComposer.cs` | Composes final stylist responses using Gemini |
| `VertexAiAdminProvider.cs` | Vertex AI client provider for admin agent workflows |
| `VertexAiImageValidationService.cs` | Validates uploaded images via Vertex AI |
| `CachedImageValidationService.cs` | Caches image validation results to reduce AI calls |
| `IntentClassifier.cs` | Classifies Vietnamese chat input intent (style/try-on/catalog/general) |
| `StylistFallbackTextService.cs` | Generates fallback text responses when AI is unavailable |
| `ThreadMemoryService.cs` | Extracts and stores facts/product refs from chat threads |
| `ChatTextUtils.cs` | Vietnamese text utilities for chat processing |
| `SafetyGate.cs` | Risk-level gate for admin AI actions; requires confirmation/approval by risk level |
| `LlmAuditService.cs` | Records raw LLM prompt/response pairs to `llm_audit_logs` |
| `PromptRedactionService.cs` | Redacts PII from prompts before audit logging |
| `AdminAgentService.cs` | Admin AI agent orchestration: tool dispatch, action audit, safety gate integration |
| `HermesAgentService.cs` | Hermes long-running agent: run lifecycle, heartbeat, trace steps, reports |
| `HermesEventOutboxPublisher.cs` | Publishes events to `hermes_event_outbox` for reliable delivery |
| `HermesEventProcessor.cs` | Processes outbox events and dispatches to listeners |
| `HermesFeedService.cs` | SSE feed of Hermes run events for real-time frontend updates |
| `HermesMonitorLinkService.cs` | Creates and resolves external monitor share links for Hermes runs |
| `BackgroundHermesOutboxWorker.cs` | Background worker polling `hermes_event_outbox` for undelivered events |
| `AdminChatPersistence.cs` | Persists admin AI chat threads and messages |
| `AutoModeStore.cs` | In-memory store for admin AI auto-mode state per session |
| `PendingActionStore.cs` | In-memory store for pending admin AI actions awaiting approval |
| `AdminToolRiskService.cs` | CRUD for `tool_risk_configs`; overrides default risk levels per tool |
| `S3StorageService.cs` | S3-compatible file upload/delete/URL generation (AWS S3 and MinIO) |
| `UploadStoragePathResolver.cs` | Resolves storage paths and public URLs for uploaded files |
| `ImageUploadValidator.cs` | Validates image file type, size, and dimensions before upload |
| `ImageVisibilityService.cs` | Toggles product image visibility flags |
| `BlogImageVisibilityService.cs` | Toggles blog image visibility flags |
| `EmailTemplateService.cs` | Renders email templates with variable substitution |
| `EmailQueueService.cs` | Enqueues email jobs to `email_jobs` table |
| `BackgroundEmailWorker.cs` | Background worker that sends queued email jobs via SMTP |
| `SubscriberService.cs` | Newsletter subscribe/unsubscribe with consent tracking |
| `MarketingConsentService.cs` | Records and queries marketing consent per user |
| `CustomerEventService.cs` | Records behavioural customer events for marketing automation |
| `OrderAttributionService.cs` | Records and queries marketing channel attribution on orders |
| `BlogAiDraftService.cs` | Generates AI-assisted blog post drafts via Gemini |
| `AdminProductService.cs` | Admin CRUD for products, variants, images |
| `AdminCategoryService.cs` | Admin CRUD for categories |
| `AdminOrderService.cs` | Admin order management: status transitions, listing, detail |
| `AdminUserService.cs` | Admin user management: listing, status changes, role assignment |
| `AdminRoleService.cs` | Admin role management |
| `AdminMediaService.cs` | Admin media upload coordination |
| `AdminDashboardService.cs` | Dashboard stats: revenue, orders, users, top products |
| `AdminInventoryService.cs` | Inventory overview and stock adjustment |
| `AdminPromoService.cs` | Admin promo code CRUD and usage stats |
| `AdminMarketingServices.cs` | Admin marketing campaign management |
| `AdminReviewService.cs` | Admin review moderation |
| `AiTryOnFeedbackService.cs` | Records and queries user feedback on AI try-on results |
| `FusionCacheService.cs` | FusionCache wrapper: L1 memory + L2 Redis with graceful degradation |
| `ConversationStore.cs` | In-memory conversation state store for chat sessions |
| `ConcurrencyLimitedAiTryOnService.cs` | Semaphore-limited wrapper for AI try-on to prevent overload |
| `ConcurrencyLimitedStylistChatService.cs` | Semaphore-limited wrapper for stylist chat |

## Hot Paths
- `StylistChatService`: main chat orchestrator — thread lifecycle → intent classification → catalog/try-on/tool calls → persistence → Gemini composer/fallback text.
- `CatalogTryOnService`: builds catalog asset set and delegates image generation to `VertexAiTryOnService`.
- `AuthService`: owns register/login/OAuth/session/cookie token flow; controllers only set/clear cookies.
- `EmailQueueService` + `BackgroundEmailWorker`: async marketing/template email pipeline.
- `HermesAgentService` + `BackgroundHermesOutboxWorker`: admin control-plane AI agent with reliable event delivery.

## For AI Agents
### Working In This Directory
- Register all new service implementations in `Api/Configuration/ServiceRegistration.cs` (not here).
- Use primary constructor DI: `public class FooService(AppDbContext db, ILogger<FooService> logger)`.
- Return result objects for expected business failures instead of throwing exceptions.
- Vietnamese text processing belongs in `IntentClassifier`, `ChatTextUtils`, fallback/composer services — not scattered across services.
- Storage public URLs must flow through `UploadStoragePathResolver`/`S3StorageService`.
- Cache keys and invalidation should go through `FusionCacheService`; avoid direct Redis calls.

### Common Patterns
- Primary constructor DI throughout.
- `AsNoTracking()` for read queries in services that call `AppDbContext` directly.
- `CancellationToken` threading through async AI/storage calls.
- Concurrency-limited AI services (`ConcurrencyLimited*`) wrap the real service with a `SemaphoreSlim`.
- Background workers implement `BackgroundService`; use `IServiceScopeFactory` for scoped dependencies.

## Dependencies
### Internal
- `Data/AppDbContext.cs` (direct EF access)
- `Repositories/` (via injected interfaces)
- `Configuration/` (GoogleCloudOptions, S3StorageSettings)
- `AoDaiNhaUyen.Domain` (entities, enums)
- `AoDaiNhaUyen.Application` (service/repo interfaces)
### External
- EF Core, Npgsql, AWS SDK S3, Google Cloud / Vertex AI SDK, FusionCache, StackExchange.Redis, MailKit/SMTP client, `System.IdentityModel.Tokens.Jwt`

<!-- MANUAL: -->
