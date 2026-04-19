<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# backend

## Purpose
ASP.NET Core 10 REST API following clean architecture. Four projects: Api (presentation), Application (use cases/DTOs), Domain (entities), Infrastructure (data access/external services). Uses EF Core with PostgreSQL, JWT authentication with Google/Facebook OAuth, MailKit for email, and Google Vertex AI for virtual try-on and stylist chat.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.slnx` | Solution file linking all backend projects |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AoDaiNhaUyen.Api/` | ASP.NET Core web API host -- controllers, middleware, configuration (see `AoDaiNhaUyen.Api/AGENTS.md`) |
| `AoDaiNhaUyen.Application/` | Application layer -- DTOs, interfaces, service implementations (see `AoDaiNhaUyen.Application/AGENTS.md`) |
| `AoDaiNhaUyen.Domain/` | Domain layer -- entities, seed data (see `AoDaiNhaUyen.Domain/AGENTS.md`) |
| `AoDaiNhaUyen.Infrastructure/` | Infrastructure layer -- EF Core, repositories, external services (see `AoDaiNhaUyen.Infrastructure/AGENTS.md`) |
| `AoDaiNhaUyen.Tests/` | Unit and integration tests (see `AoDaiNhaUyen.Tests/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Clean architecture: Api -> Application -> Infrastructure; Domain is standalone (no dependencies on other projects)
- Dependency flow: Api references Application + Infrastructure; Infrastructure references Application + Domain; Application references Domain
- Run `dotnet build` from here to compile all projects
- Run `dotnet test` to execute tests
- EF Core migrations: `dotnet ef migrations add <Name>` from Infrastructure project with `--startup-project ../AoDaiNhaUyen.Api`
- All API responses use standard envelope: `{ success, message, data, errors, timestamp }`
- .NET 10 with nullable reference types enabled
- Column names in PostgreSQL use snake_case (configured in AppDbContext.OnModelCreating)

### Testing Requirements
- Run `dotnet test` before committing
- Tests use xUnit with InMemoryDatabase for service integration tests
- Test stubs are inline private classes within each test file

### Common Patterns
- Repository pattern for data access (ICategoryRepository, IProductRepository, ICartRepository, IUserProfileRepository)
- Service layer for business logic (AuthService, CartService, CheckoutService, CatalogService, etc.)
- DTOs for request/response mapping
- JWT Bearer authentication with Google/Facebook OAuth providers
- Token stored in HttpOnly cookies (access_token + refresh_token)
- Options pattern with ValidateDataAnnotations for configuration (JwtSettings, EmailSettings, GoogleOAuthSettings, FacebookOAuthSettings, CookieSettings)

## Dependencies
### External
- ASP.NET Core 10, EF Core 10, Npgsql (PostgreSQL)
- Microsoft.AspNetCore.Authentication.JwtBearer
- MailKit (SMTP email)
- DotNetEnv (.env loading)
- Google Vertex AI (virtual try-on via Gemini, stylist chat via Gemini Flash Lite)
