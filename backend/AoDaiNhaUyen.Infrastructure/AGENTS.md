<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen.Infrastructure

## Purpose
Infrastructure layer containing EF Core database context, migrations, repositories, and all service implementations (auth, OAuth, AI try-on, stylist chat). This project references Application (for interfaces) and Domain (for entities).

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Infrastructure.csproj` | Project file -- references Application and Domain |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Infrastructure-level options (GoogleCloudOptions) |
| `Data/` | DbContext, migrations, seed service (see `Data/AGENTS.md`) |
| `Repositories/` | Repository implementations (see `Repositories/AGENTS.md`) |
| `Services/` | Service implementations (see `Services/AGENTS.md`) |

## Configuration
| File | Description |
|------|-------------|
| `GoogleCloudOptions.cs` | Google Cloud settings: ApiKey, ProjectId, Location, VirtualTryOnModel (Gemini 3.1 Flash), StylistTextModel (Gemini 3.1 Flash Lite), TimeoutSeconds |

## Data
| File | Description |
|------|-------------|
| `AppDbContext.cs` | EF Core DbContext with all DbSets, entity configuration in OnModelCreating (snake_case columns, constraints, indexes, relationships) |
| `AppDbContextFactory.cs` | Design-time factory for EF Core migrations CLI |
| `SeedDataService.cs` | Database seeder: migrates on startup, seeds roles, customers, categories, products, style scenarios, style profiles, and AI assets |
| `Migrations/` | EF Core migration files (see `Data/Migrations/AGENTS.md`) |

### AppDbContext Details
- All table names use snake_case (e.g., `users`, `product_variants`, `chat_messages`)
- Column names are auto-converted to snake_case via `ApplySnakeCaseColumnNames()`
- Uses PostgreSQL enums for order_status and shipping_status
- IpAddress on UserSession is stored as PostgreSQL `inet` type
- JSON columns use `jsonb` PostgreSQL type
- Extensive check constraints for status values, price bounds, rating ranges

### SeedDataService Details
- Called on startup when `RunMigrationsAndSeedOnStartup` is true
- Upsert pattern: updates existing records if they already exist
- Seeds in order: Roles -> Customers -> Categories -> Products -> StyleScenarios -> ProductStyleData -> ProductAiAssets
- Removes stale products/categories that are no longer in seed data
- AI assets are resolved from product images and curated files in `upload/tryon-curated/`
- Vietnamese scenario names: Giao vien, Le Tet, Du tiec, Chup anh
- Infers color families, formality, style keywords, and scenario scores from product slugs/categories

## Repositories
| File | Description |
|------|-------------|
| `CategoryRepository.cs` | Implements ICategoryRepository -- fetches active categories |
| `ProductRepository.cs` | Implements IProductRepository -- paged product listing with filters (category, type, featured, size), slug lookup with includes |
| `CartRepository.cs` | Implements ICartRepository -- cart operations with items and variants |
| `UserProfileRepository.cs` | Implements IUserProfileRepository -- user data with addresses, orders, order items |

## Services
| File | Description |
|------|-------------|
| `AuthService.cs` | Full auth flow: register with email verification, login, Google/Facebook OAuth, refresh tokens, logout, password reset |
| `CartService.cs` | Cart business logic: add/update/remove items, clear cart |
| `CheckoutService.cs` | Order placement: creates order with items from cart, generates order code |
| `UserService.cs` | User profile CRUD, address management, order history |
| `CatalogStylingService.cs` | Product recommendation engine for stylist chat: recommend by scenario/budget/color, lookup, compare |
| `CatalogTryOnService.cs` | AI try-on catalog management and image generation orchestration |
| `ChatTextUtils.cs` | Vietnamese text utilities: normalization, budget extraction, keyword detection |
| `FacebookOAuthService.cs` | Facebook OAuth code exchange: trades code for access token, fetches user info |
| `GoogleOAuthService.cs` | Google OAuth code exchange: trades code for token, fetches user info from OpenID Connect |
| `IntentClassifier.cs` | Chat intent detection: classifies Vietnamese messages into intents (outfit_recommendation, tryon_prepare, tryon_execute, catalog_lookup, etc.) with scenario/color/material extraction |
| `JwtTokenService.cs` | JWT generation for access tokens, email verification tokens, password reset tokens |
| `Pbkdf2PasswordHasher.cs` | Password hashing using PBKDF2 with verification |
| `RefreshTokenService.cs` | Refresh token generation and hashing |
| `StylistChatService.cs` | Main chat orchestrator: manages threads, classifies intents, routes to appropriate handler, persists memory |
| `ThreadMemoryService.cs` | Chat thread memory management: reads/writes structured state to ChatThreadMemory entity |
| `VertexAiStylistResponseComposer.cs` | Calls Google Gemini to compose stylist responses in Vietnamese |
| `VertexAiTryOnService.cs` | Calls Google Vertex AI Virtual Try-On API for image generation |

## For AI Agents
### Working In This Directory
- This is where all business logic and data access lives
- Services implement interfaces defined in `AoDaiNhaUyen.Application/Interfaces/`
- Repositories implement interfaces in `AoDaiNhaUyen.Application/Interfaces/Repositories/`
- New repositories should be registered in `AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs`
- Migrations are auto-applied on startup via `SeedDataService.SeedAllAsync()` -> `dbContext.Database.MigrateAsync()`
- IntentClassifier uses Vietnamese keyword matching (not ML) -- extend via new keyword patterns
- ChatTextUtils handles Vietnamese-specific text processing (budget in VND, Vietnamese color/material names)
- GoogleCloudOptions configures two separate Gemini models: one for try-on image generation, one for text chat
