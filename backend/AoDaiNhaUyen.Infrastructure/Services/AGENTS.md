<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Services

## Purpose
Service implementations for all business logic. Each service implements an interface from `AoDaiNhaUyen.Application/Interfaces/Services/`. Services coordinate between repositories, external APIs, and domain logic.

## Files
| File | Interface | Description |
|------|-----------|-------------|
| `AuthService.cs` | `IAuthService` | Full authentication flow: register with email verification, credential login, Google/Facebook OAuth login, refresh token rotation, logout, email verification, password reset, get current user |
| `CartService.cs` | `ICartService` | Shopping cart logic: get cart with item details, add item (dedup by variant), update quantity, remove item, clear cart |
| `CheckoutService.cs` | `ICheckoutService` | Order placement: validates address, creates order with items, generates unique order code, clears cart on success |
| `UserService.cs` | `IUserService` | User profile read/update, address CRUD (create/delete), order history with pagination and order items |
| `CatalogStylingService.cs` | `ICatalogStylingService` | Product recommendation engine for stylist chat: recommend by scenario/budget/color/material, catalog search, product comparison, resolve product references from text |
| `CatalogTryOnService.cs` | `ICatalogTryOnService` | AI try-on orchestration: builds catalog from ProductAiAsset entities, delegates image generation to VertexAiTryOnService, handles accessory matching |
| `ChatTextUtils.cs` | (internal utility) | Vietnamese text processing: normalization (lowercase, remove diacritics), budget extraction (VND amounts), keyword-based detection helpers |
| `FacebookOAuthService.cs` | `IFacebookOAuthService` | Facebook OAuth: exchanges authorization code for access token via `/oauth/access_token`, fetches user info from `/me` endpoint |
| `GoogleOAuthService.cs` | `IGoogleOAuthService` | Google OAuth: exchanges authorization code for token via `oauth2.googleapis.com/token`, fetches user info from OpenID Connect userinfo endpoint |
| `IntentClassifier.cs` | `IIntentClassifier` | Chat intent detection using Vietnamese keyword matching: classifies into outfit_recommendation, tryon_prepare, tryon_execute, catalog_lookup, product_comparison, image_style_analysis, out_of_scope, clarification. Extracts scenario, color, material, budget from Vietnamese text |
| `JwtTokenService.cs` | `IJwtTokenService` | JWT token generation with claims (userId, email, roles), email verification tokens, password reset tokens with HMAC-based signing |
| `Pbkdf2PasswordHasher.cs` | `IPasswordHasher` | Password hashing using PBKDF2 with SHA256, 600000 iterations. Verification supports rehash on success |
| `RefreshTokenService.cs` | `IRefreshTokenService` | Generates cryptographically random refresh tokens, hashes with SHA256 for storage |
| `StylistChatService.cs` | `IStylistChatService` | Main chat orchestrator: manages thread lifecycle, classifies user intent, routes to recommendation/try-on/clarification handlers, persists messages and thread memory, coordinates with AI response composer |
| `ThreadMemoryService.cs` | `IThreadMemoryService` | Manages per-thread conversation state: extracts facts (scenario, budget, color, material) from Vietnamese user messages, serializes/deserializes memory to/from ChatThreadMemory entity |
| `VertexAiStylistResponseComposer.cs` | `IStylistResponseComposer` | Calls Google Gemini Flash Lite API to compose stylist responses in Vietnamese, sends conversation context and structured payload |
| `VertexAiTryOnService.cs` | `IAiTryOnService` | Calls Google Vertex AI Virtual Try-On API (Gemini Flash Image) with person and garment images, returns generated try-on image |

## For AI Agents
### Working In This Directory
- All services are registered as scoped in `AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs`
- AI services (VertexAiTryOnService, VertexAiStylistResponseComposer) are registered as typed HttpClients with infinite timeout
- Services use primary constructor injection
- Result types follow pattern: `{ Succeeded: bool, Value: T?, ErrorCode: string?, ErrorMessage: string? }`
- Vietnamese context: IntentClassifier and ChatTextUtils process Vietnamese text with diacritics
- IntentClassifier uses rule-based matching (not ML) -- extend by adding keyword patterns to the `normalized` switch and detection methods
- AuthService handles three auth providers: credentials (email/password), Google OAuth, Facebook OAuth
- StylistChatService is the main orchestrator for the AI chat feature -- it coordinates IntentClassifier, ThreadMemoryService, CatalogStylingService, CatalogTryOnService, and VertexAiStylistResponseComposer
