<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Services

## Purpose
Service implementations for business logic. Each service implements interface from `AoDaiNhaUyen.Application/Interfaces/Services/`. Services coordinate repositories, external APIs, domain logic.

## Files
| File | Interface | Description |
|------|-----------|-------------|
| `AuthService.cs` | `IAuthService` | Full auth flow: register with email verification, credential login, Google/Facebook OAuth login, refresh token rotation, logout, email verification, password reset, get current user |
| `CartService.cs` | `ICartService` | Shopping cart logic: get cart with item details, add item (dedup by variant), update quantity, remove item, clear cart |
| `CheckoutService.cs` | `ICheckoutService` | Order placement: validate address, create order with items, generate unique order code, clear cart on success |
| `UserService.cs` | `IUserService` | User profile read/update, address CRUD (create/delete), order history with pagination and order items |
| `CachedImageValidationService.cs` | `ICachedImageValidationService` | Cached image validation results for repeated upload and URL checks |
| `CatalogStylingService.cs` | `ICatalogStylingService` | Product recommendation engine for stylist chat: recommend by scenario/budget/color/material, catalog search, product comparison, resolve product refs from text |
| `CatalogTryOnService.cs` | `ICatalogTryOnService` | AI try-on orchestration: build catalog from ProductAiAsset entities, delegate image generation to VertexAiTryOnService, handle accessory matching |
| `ChatTextUtils.cs` | (internal utility) | Vietnamese text processing: normalization (lowercase, remove diacritics), budget extraction (VND amounts), keyword detection helpers |
| `FacebookOAuthService.cs` | `IFacebookOAuthService` | Facebook OAuth: exchange authorization code for access token via `/oauth/access_token`, fetch user info from `/me` endpoint |
| `GoogleOAuthService.cs` | `IGoogleOAuthService` | Google OAuth: exchange authorization code for token via `oauth2.googleapis.com/token`, fetch user info from OpenID Connect userinfo endpoint |
| `ImageValidationService.cs` | `IImageValidationService` | Image validation orchestration for uploads and image refs before AI generation |
| `IntentClassifier.cs` | `IIntentClassifier` | Chat intent detection using Vietnamese keyword matching: classify into outfit_recommendation, tryon_prepare, tryon_execute, catalog_lookup, product_comparison, image_style_analysis, out_of_scope, clarification. Extract scenario, color, material, budget from Vietnamese text |
| `JwtTokenService.cs` | `IJwtTokenService` | JWT token generation with claims (userId, email, roles), email verification tokens, password reset tokens with HMAC-based signing |
| `Pbkdf2PasswordHasher.cs` | `IPasswordHasher` | Password hashing using PBKDF2 with SHA256, 600000 iterations. Verification supports rehash on success |
| `RefreshTokenService.cs` | `IRefreshTokenService` | Generate cryptographically random refresh tokens, hash with SHA256 for storage |
| `StylistChatService.cs` | `IStylistChatService` | Main chat orchestrator: manage thread lifecycle, classify user intent, route to recommendation/try-on/clarification handlers, persist messages and thread memory, coordinate with AI response composer |
| `StylistFallbackTextService.cs` | `IStylistFallbackTextService` | Deterministic Vietnamese stylist responses when AI composition unavailable |
| `ThreadMemoryService.cs` | `IThreadMemoryService` | Manage per-thread conversation state: extract facts (scenario, budget, color, material) from Vietnamese user messages, serialize/deserialize memory to/from ChatThreadMemory entity |
| `UploadStoragePathResolver.cs` | `IUploadStoragePathResolver` | Resolve upload storage paths and public URLs for chat and try-on assets |
| `VertexAiImageValidationService.cs` | `IImageValidationService` | Call Vertex AI/Gemini to validate uploaded images and referenced catalog images |
| `VertexAiStylistResponseComposer.cs` | `IStylistResponseComposer` | Call Google Gemini Flash Lite API to compose stylist responses in Vietnamese, send conversation context and structured payload |
| `VertexAiTryOnService.cs` | `IAiTryOnService` | Call Google Vertex AI Virtual Try-On API (Gemini Flash Image) with person and garment images, return generated try-on image |
| `ZaloOAuthService.cs` | `IZaloOAuthService` | Zalo OAuth: exchange authorization code and fetch Zalo user info |

## For AI Agents
### Working In This Directory
- All services registered as scoped in `AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs`
- AI services (VertexAiTryOnService, VertexAiStylistResponseComposer) registered as typed HttpClients with infinite timeout
- Services use primary constructor injection
- Result types follow pattern: `{ Succeeded: bool, Value: T?, ErrorCode: string?, ErrorMessage: string? }`
- Vietnamese context: IntentClassifier and ChatTextUtils process Vietnamese text with diacritics
- IntentClassifier uses rule-based matching (not ML) -- extend by adding keyword patterns to `normalized` switch and detection methods
- AuthService handles three auth providers: credentials (email/password), Google OAuth, Facebook OAuth
- StylistChatService = main orchestrator for AI chat feature -- coordinates IntentClassifier, ThreadMemoryService, CatalogStylingService, CatalogTryOnService, VertexAiStylistResponseComposer