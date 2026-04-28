<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Interfaces

## Purpose
Service/repository interfaces define contracts between Application and Infrastructure layers. All implemented in `AoDaiNhaUyen.Infrastructure`.

## Root Interface
| File | Description |
|------|-------------|
| `ISeedDataService.cs` | DB seeding contract: `SeedAllAsync()` -- implemented by SeedDataService |

## Repositories/ Subdirectory
| File | Description |
|------|-------------|
| `ICategoryRepository.cs` | Category data access: `GetActiveAsync()` returns active categories |
| `IProductRepository.cs` | Product data access: `GetPagedAsync()` with filters (categorySlug, productType, featured, size), `GetBySlugAsync()` with variants/images/category includes |
| `ICartRepository.cs` | Cart data access: get user cart, add/update/remove items, clear cart |
| `IUserProfileRepository.cs` | User data access: get user with addresses/orders/order items; profile/address CRUD |

## Services/ Subdirectory
| File | Description |
|------|-------------|
| `IAiTryOnService.cs` | AI virtual try-on via Vertex AI: `TryOnAsync()` with person/garment images |
| `IAuthService.cs` | Auth: register, login, Google/Facebook OAuth, refresh, logout, email verification, password reset, get current user |
| `ICartService.cs` | Cart logic: get/add/update/remove items, clear cart |
| `ICatalogService.cs` | Product catalog: get categories (flat/tree), get products (paged/filtered), get product by slug |
| `ICatalogStylingService.cs` | Styling recs: `RecommendAsync()` by scenario/budget/color, `LookupAsync()` by query, `CompareAsync()` by product IDs, `ResolveProductReferencesAsync()` |
| `ICatalogTryOnService.cs` | Try-on catalog: `GetCatalogAsync()`, `CreateAsync()` for try-on image generation |
| `ICheckoutService.cs` | Order placement: `CheckoutAsync()` creates order from cart |
| `IEmailService.cs` | Email send: `SendEmailAsync(toEmail, subject, htmlBody)` |
| `IFacebookOAuthService.cs` | Facebook OAuth: `ExchangeCodeForUserAsync(code)` returns FacebookUserInfoDto |
| `IGoogleOAuthService.cs` | Google OAuth: `ExchangeCodeForUserAsync(code)` returns GoogleUserInfoDto |
| `IIntentClassifier.cs` | Chat intent classification: `ClassifyAsync(message, attachments, memory)` returns IntentClassificationDto |
| `IJwtTokenService.cs` | JWT ops: generate access token, generate/validate email verification/password reset tokens |
| `IPasswordHasher.cs` | Password hashing: `HashPassword()`, `VerifyHashedPassword()` |
| `IRefreshTokenService.cs` | Refresh token: `GenerateToken()`, `HashToken()` |
| `IImageValidationService.cs` | Image validation: validate uploads/image refs before AI try-on |
| `ICachedImageValidationService.cs` | Cached image validation: store/reuse validation results for repeat images |
| `IStylistChatService.cs` | Chat orchestration: list/create/get threads, add messages, execute try-on in chat |
| `IStylistFallbackTextService.cs` | Deterministic fallback Vietnamese stylist text when AI composition unavailable |
| `IStylistResponseComposer.cs` | AI response composition: `ComposeAsync()` calls Gemini for stylist text |
| `IThreadMemoryService.cs` | Thread memory: `ApplyUserTurn()`, `Persist()`, `Read()` |
| `IUploadStoragePathResolver.cs` | Upload storage path resolution for public URLs/local storage paths |
| `IUserService.cs` | User ops: profile CRUD, address management, order history |
| `IZaloOAuthService.cs` | Zalo OAuth: exchange authorization code for Zalo user info |

## For AI Agents
### Working In This Directory
- These are **contracts** -- implementations live in `AoDaiNhaUyen.Infrastructure/Services/` and `AoDaiNhaUyen.Infrastructure/Repositories/`
- All interfaces use `I` prefix convention
- Async methods return `Task<T>` with `CancellationToken` parameter
- Result types follow pattern: `{ Succeeded, Value, ErrorCode, ErrorMessage }` (e.g., AuthResult)
- New feature: define interface here, implement in Infrastructure, register in ServiceRegistration.cs
- Repositories = data-access only -- no business logic
- Services contain business logic and coordinate repositories/external APIs