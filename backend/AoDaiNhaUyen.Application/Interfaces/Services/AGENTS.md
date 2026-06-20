<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Interfaces/Services

## Purpose
All application service contracts — approximately 60 interfaces spanning auth, catalog, cart, checkout, blog, AI try-on, stylist chat, admin operations, marketing, email, storage, and infrastructure plumbing. Implementations live in `AoDaiNhaUyen.Infrastructure/Services/`.

## Key Files by Domain

### Auth & Identity
| File | Description |
|------|-------------|
| `IAuthService.cs` | Register, login, Google/Zalo OAuth, refresh token, logout, email verification, password reset, get current user |
| `IJwtTokenService.cs` | Generate access token; generate/validate email verification and password reset tokens |
| `IRefreshTokenService.cs` | `GenerateToken()`, `HashToken()` for refresh token lifecycle |
| `IPasswordHasher.cs` | `HashPassword()`, `VerifyHashedPassword()` |
| `IGoogleOAuthService.cs` | `ExchangeCodeForUserAsync(code)` → `GoogleUserInfoDto` |
| `IZaloOAuthService.cs` | `ExchangeCodeForUserAsync(code)` → `ZaloUserInfoDto` |
| `IUserService.cs` | Profile CRUD, address management, order history for authenticated users |

### Catalog & Products
| File | Description |
|------|-------------|
| `ICatalogService.cs` | Get categories (flat/tree), get products (paged/filtered), get product by slug |
| `ICatalogStylingService.cs` | AI styling recommendations: `RecommendAsync`, `LookupAsync`, `CompareAsync`, `ResolveProductReferencesAsync` |
| `ICatalogTryOnService.cs` | Try-on catalog: `GetCatalogAsync()`, `CreateAsync()` for AI image generation |
| `IImageVisibilityService.cs` | Resolve private S3 URLs to presigned/public URLs for product images |
| `IStockService.cs` | Stock level queries and reservation |

### Cart & Checkout
| File | Description |
|------|-------------|
| `ICartService.cs` | Get, add, update, remove, clear cart items |
| `ICheckoutService.cs` | `CheckoutAsync()` — creates order from cart, validates stock, applies promo |
| `IPromoService.cs` | `ValidateAsync(code, subtotal)` → `PromoValidationResult` |
| `IPromoCostService.cs` | Promo cost/discount calculation helpers |
| `IOrderService.cs` | User-facing order queries (distinct from admin order service) |
| `IOrderAttributionService.cs` | Link orders to marketing campaigns for attribution |

### Blog
| File | Description |
|------|-------------|
| `IBlogPostService.cs` | Blog post CRUD, paged list with filters, get by slug |
| `IBlogCategoryService.cs` | Blog category CRUD |
| `IBlogAiDraftService.cs` | Generate draft blog post via LLM from topic/keywords |
| `IBlogImageVisibilityService.cs` | Resolve blog image URLs (presigned/public) |

### AI Try-On & Image
| File | Description |
|------|-------------|
| `IAiTryOnService.cs` | `TryOnAsync()` — Vertex AI virtual try-on with person/garment images |
| `IAiTryOnFeedbackService.cs` | Save and retrieve user feedback on try-on results |
| `IImageValidationService.cs` | Validate image uploads before AI processing (size, dimensions, format) |
| `ICachedImageValidationService.cs` | Cache-backed image validation to avoid re-validating known images |
| `IImageUploadValidator.cs` | Synchronous upload validation (MIME type, extension, size) |

### Stylist Chat
| File | Description |
|------|-------------|
| `IStylistChatService.cs` | List/create/get threads, add messages, execute try-on within chat |
| `IStylistResponseComposer.cs` | `ComposeAsync()` — calls Gemini to generate stylist response text |
| `IStylistFallbackTextService.cs` | Deterministic Vietnamese fallback text when AI composition is unavailable |
| `IIntentClassifier.cs` | `ClassifyAsync(message, attachments, memory)` → `IntentClassificationDto` |
| `IThreadMemoryService.cs` | `ApplyUserTurn()`, `Persist()`, `Read()` for per-thread conversation memory |
| `IConversationStore.cs` | Persistent store for chat thread messages |
| `IPendingActionStore.cs` | Store for pending actions awaiting user confirmation in chat flows |
| `IAutoModeStore.cs` | Store for auto-mode state per chat thread |

### Admin Services
| File | Description |
|------|-------------|
| `IAdminProductService.cs` | Admin product CRUD: create/update/delete, variant stock, image management |
| `IAdminCategoryService.cs` | Admin category CRUD and ordering |
| `IAdminOrderService.cs` | Admin order status transitions, shipment management |
| `IAdminUserService.cs` | Admin user management: list, create, update, status toggle |
| `IAdminRoleService.cs` | Role CRUD |
| `IAdminPromoService.cs` | Promo code CRUD, toggle active/inactive |
| `IAdminInventoryService.cs` | Low-stock alerts, inventory reports |
| `IAdminDashboardService.cs` | Analytics: summary KPIs, revenue chart, order distribution, top products, user growth |
| `IAdminMediaService.cs` | Image upload/delete for products and blog |
| `IAdminReviewService.cs` | Admin moderation of user reviews |
| `IAdminMarketingServices.cs` | Campaign management, subscriber stats, send-job management |
| `IAdminAgentService.cs` | Admin AI agent operations (distinct from Hermes) |
| `IAdminLlmProvider.cs` | LLM call provider for admin agent tools |
| `IAdminChatPersistence.cs` | Persistence for admin agent chat history |
| `IAdminToolRiskService.cs` | Risk assessment for admin agent tool calls |
| `ILlmAuditService.cs` | Audit log for all LLM calls made by admin agent |

### Hermes Agent
| File | Description |
|------|-------------|
| `IHermesAgentService.cs` | Hermes control plane: status, heartbeat, streaming chat, run history, reports |
| `IHermesEventOutboxService.cs` | Outbox event management: list, retry, status queries |
| `IHermesEventOutboxPublisher.cs` | Publish domain events to the Hermes outbox for processing |
| `IHermesEventProcessor.cs` | Process individual outbox events (called by outbox worker) |
| `IHermesFeedService.cs` | Live feed of recent Hermes activity for admin dashboard |
| `IHermesMonitorLinkService.cs` | Generate monitor links for Hermes run tracking |

### Infrastructure / Cross-Cutting
| File | Description |
|------|-------------|
| `IStorageService.cs` | File upload/delete on S3-compatible storage |
| `IUploadStoragePathResolver.cs` | Resolve public URLs and local paths for uploaded files |
| `IEmailService.cs` | `SendEmailAsync(toEmail, subject, htmlBody)` |
| `IEmailTemplateService.cs` | Render email templates (verification, password reset, order confirm) |
| `IEmailQueueService.cs` | Queue emails for background delivery |
| `ISubscriberService.cs` | Email newsletter subscription management |
| `ICustomerEventService.cs` | Track customer behavioral events for marketing attribution |
| `IMarketingConsentService.cs` | GDPR/consent management for marketing communications |
| `IPromptRedactionService.cs` | Redact PII from LLM prompts before logging |

## For AI Agents

### Working In This Directory
- All interfaces use the `I` prefix
- Async methods take `CancellationToken` as the last parameter with `= default`
- Streaming methods return `IAsyncEnumerable<T>` (e.g., `IHermesAgentService.StreamChatAsync`)
- Result types use the `{ Succeeded, Value, ErrorCode, ErrorMessage }` pattern
- New interfaces: declare here → implement in Infrastructure → register in `Infrastructure/ServiceRegistration.cs`

### Common Patterns
```csharp
// Standard async service method:
Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);

// Streaming method:
IAsyncEnumerable<HermesStreamChunk> StreamChatAsync(
    HermesChatRequest request, Guid adminUserId, CancellationToken ct);

// Paged query:
Task<PagedResult<BlogPostListItemDto>> GetPostsAsync(
    BlogPostStatus? status, string? tag, string? categorySlug,
    string? search, int page, int pageSize, CancellationToken ct = default);
```

## Dependencies
### Internal
- Consumed by: `AoDaiNhaUyen.Api` controllers and `AoDaiNhaUyen.Application.Services`
- Implemented by: `AoDaiNhaUyen.Infrastructure/Services/`
- DTOs exchanged: all from `AoDaiNhaUyen.Application.DTOs`

<!-- MANUAL: -->
