<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen.Api

## Purpose
ASP.NET Core 10 web API host. Contains controllers, middleware, response types, API-level services, and application configuration. This is the entry point -- Program.cs bootstraps the entire application.

## Key Files
| File | Description |
|------|-------------|
| `Program.cs` | Application entry point: loads .env, configures CORS, static files, auth, DI container, and seed-on-startup |
| `AoDaiNhaUyen.Api.csproj` | Project file with NuGet references |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Service registration extension method for DI (see below) |
| `Controllers/` | API controllers -- thin, delegate to Application services (see `Controllers/AGENTS.md`) |
| `Middleware/` | Global exception handling middleware |
| `Responses/` | ApiResponse envelope types and factory |
| `Services/` | API-host-level service implementations (SmtpEmailService) |
| `upload/` | Static product images served at `/upload/` |
| `upload/tryon-curated/` | Curated AI try-on garment/accessory images |

## Configuration
| File | Description |
|------|-------------|
| `ServiceRegistration.cs` | `AddBackendServices()` extension: registers DbContext, JWT auth, all repositories, services, and options with validation |

### What ServiceRegistration.cs Configures
- **Database**: Npgsql/PostgreSQL via connection string from config or env var `ConnectionStrings__DefaultConnection`
- **Authentication**: JWT Bearer with cookie-based token extraction (OnMessageReceived reads from HttpOnly cookie)
- **Options with validation**: JwtSettings, EmailSettings, GoogleOAuthSettings, FacebookOAuthSettings, CookieSettings (all validated on startup)
- **Repositories**: CategoryRepository, CartRepository, ProductRepository, UserProfileRepository
- **Services**: CatalogService, CartService, CheckoutService, UserService, AuthService, all OAuth services, AI try-on services, stylist chat services
- **HttpClients**: VertexAiTryOnService, VertexAiStylistResponseComposer (with infinite timeout for long AI calls)

## Controllers
| Controller | Route | Description |
|-----------|-------|-------------|
| `AiTryOnController` | `api/v1/ai-tryon` | AI virtual try-on: catalog listing and image generation via Vertex AI |
| `AuthController` | `api/auth` | Full auth flow: register, login, Google/Facebook OAuth, email verification, password reset, refresh tokens |
| `CategoriesController` | `api/v1/categories` | Category listing (flat and header tree) |
| `ChatController` | `api/v1/chat/threads` | AI stylist chat: thread management, messaging with attachments, in-chat try-on |
| `CheckoutController` | `api/users/me/checkout` | Order placement (requires auth) |
| `HealthController` | `health` | Health check endpoint |
| `ProductsController` | `api/v1/products` | Product catalog: paginated listing with filters, detail by slug |
| `UserAddressController` | `api/users/me/addresses` | User address CRUD (requires auth) |
| `UserCartController` | `api/users/me/cart` | Shopping cart operations (requires auth) |
| `UserController` | `api/users/me` | User profile read/update (requires auth) |
| `UserOrderController` | `api/users/me/orders` | User order history with pagination (requires auth) |

## Middleware
| File | Description |
|------|-------------|
| `ExceptionHandlingMiddleware.cs` | Catches all unhandled exceptions, logs them, returns 500 with `ApiResponseFactory.Failure("Co loi xay ra", ...)` |

## Responses
| File | Description |
|------|-------------|
| `ApiResponse.cs` | `ApiResponse<T>` and `PaginatedApiResponse<T>` record types -- standard JSON envelope |
| `ApiResponseFactory.cs` | Static factory: `Success<T>()`, `PaginatedSuccess<T>()`, `Failure()` -- default Vietnamese success message "Lay du lieu thanh cong" |
| `ApiError.cs` | `ApiError` record with Code and Message fields |

## Services
| File | Description |
|------|-------------|
| `SmtpEmailService.cs` | IEmailService implementation using MailKit/SMTP for sending verification and password reset emails |

## For AI Agents
### Working In This Directory
- Controllers are thin -- they only handle HTTP concerns (parsing, validation, response shaping) and delegate all business logic to Application services
- All responses use `ApiResponse<T>` or `PaginatedApiResponse<T>` envelope from `Responses/` via `ApiResponseFactory`
- JWT auth configured in `Configuration/ServiceRegistration.cs` -- tokens are read from HttpOnly cookies, not Authorization headers
- CORS policy "Frontend" allows configured origins from `FrontendOrigins` config (defaults to localhost:5173 and production domains)
- Static files served from `upload/` directory at `/upload` path
- Error messages in controllers are in Vietnamese (e.g., "Dang ky that bai", "Khong tim thay du lieu")
- AuthController writes access_token (60min, path `/`) and refresh_token (30d, path `/api/auth`) as HttpOnly cookies
- ChatController supports both authenticated users and anonymous guests (via `stylist_guest` cookie)
- AiTryOnController has 8MB per-image limit, max 3 accessory images, supports both uploaded garment images and catalog product selection

### Adding a New Controller
1. Create in `Controllers/` with `[ApiController]` and `[Route("api/...")]`
2. Inject Application-layer service interfaces via primary constructor
3. Use `ApiResponseFactory.Success/Failure` for all responses
4. Add `[Authorize]` for endpoints requiring authentication
5. Register the backing service in `Configuration/ServiceRegistration.cs`
