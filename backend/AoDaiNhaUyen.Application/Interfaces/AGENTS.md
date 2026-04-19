<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Interfaces

## Purpose
Service and repository interfaces that define the contracts between the Application and Infrastructure layers. All interfaces are implemented in `AoDaiNhaUyen.Infrastructure`.

## Root Interface
| File | Description |
|------|-------------|
| `ISeedDataService.cs` | Contract for database seeding: `SeedAllAsync()` -- implemented by SeedDataService |

## Repositories/ Subdirectory
| File | Description |
|------|-------------|
| `ICategoryRepository.cs` | Category data access: `GetActiveAsync()` returns active categories |
| `IProductRepository.cs` | Product data access: `GetPagedAsync()` with filters (categorySlug, productType, featured, size), `GetBySlugAsync()` with includes for variants, images, category |
| `ICartRepository.cs` | Cart data access: get cart by user, add/update/remove items, clear cart |
| `IUserProfileRepository.cs` | User data access: get user with addresses, orders, order items; profile and address CRUD |

## Services/ Subdirectory
| File | Description |
|------|-------------|
| `IAiTryOnService.cs` | AI virtual try-on via Vertex AI: `TryOnAsync()` with person/garment images |
| `IAuthService.cs` | Authentication: register, login, Google/Facebook OAuth, refresh, logout, email verification, password reset, get current user |
| `ICartService.cs` | Cart business logic: get/add/update/remove items, clear cart |
| `ICatalogService.cs` | Product catalog: get categories (flat/tree), get products (paged/filtered), get product by slug |
| `ICatalogStylingService.cs` | Styling recommendations: `RecommendAsync()` by scenario/budget/color, `LookupAsync()` by query, `CompareAsync()` by product IDs, `ResolveProductReferencesAsync()` |
| `ICatalogTryOnService.cs` | Try-on catalog: `GetCatalogAsync()`, `CreateAsync()` for generating try-on images |
| `ICheckoutService.cs` | Order placement: `CheckoutAsync()` creates order from cart |
| `IEmailService.cs` | Email sending: `SendEmailAsync(toEmail, subject, htmlBody)` |
| `IFacebookOAuthService.cs` | Facebook OAuth: `ExchangeCodeForUserAsync(code)` returns FacebookUserInfoDto |
| `IGoogleOAuthService.cs` | Google OAuth: `ExchangeCodeForUserAsync(code)` returns GoogleUserInfoDto |
| `IIntentClassifier.cs` | Chat intent classification: `ClassifyAsync(message, attachments, memory)` returns IntentClassificationDto |
| `IJwtTokenService.cs` | JWT operations: generate access token, generate/validate email verification and password reset tokens |
| `IPasswordHasher.cs` | Password hashing: `HashPassword()`, `VerifyHashedPassword()` |
| `IRefreshTokenService.cs` | Refresh token: `GenerateToken()`, `HashToken()` |
| `IStylistChatService.cs` | Chat orchestration: list/create/get threads, add messages, execute try-on in chat |
| `IStylistResponseComposer.cs` | AI response composition: `ComposeAsync()` calls Gemini to generate stylist text |
| `IThreadMemoryService.cs` | Thread memory management: `ApplyUserTurn()`, `Persist()`, `Read()` |
| `IUserService.cs` | User operations: profile CRUD, address management, order history |

## For AI Agents
### Working In This Directory
- These are the **contracts** -- actual implementations live in `AoDaiNhaUyen.Infrastructure/Services/` and `AoDaiNhaUyen.Infrastructure/Repositories/`
- All interfaces use `I` prefix convention
- Async methods return `Task<T>` with `CancellationToken` parameter
- Result types follow a pattern: `{ Succeeded, Value, ErrorCode, ErrorMessage }` (e.g., AuthResult)
- When adding a new feature: define the interface here, implement in Infrastructure, register in ServiceRegistration.cs
- Repositories are data-access only -- no business logic
- Services contain business logic and coordinate between repositories and external APIs
