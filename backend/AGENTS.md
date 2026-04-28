<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# backend

## Purpose
ASP.NET Core 10 REST API, clean architecture. Four projects: Api (presentation), Application (use cases/DTOs), Domain (entities), Infrastructure (data access/external services). Uses EF Core with PostgreSQL, JWT auth with Google/Facebook OAuth, MailKit email, Google Vertex AI for virtual try-on + stylist chat.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.slnx` | Solution file linking all backend projects |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `AoDaiNhaUyen.Api/` | ASP.NET Core web API host -- controllers, middleware, config (see `AoDaiNhaUyen.Api/AGENTS.md`) |
| `AoDaiNhaUyen.Application/` | Application layer -- DTOs, interfaces, services (see `AoDaiNhaUyen.Application/AGENTS.md`) |
| `AoDaiNhaUyen.Domain/` | Domain layer -- entities, seed data (see `AoDaiNhaUyen.Domain/AGENTS.md`) |
| `AoDaiNhaUyen.Infrastructure/` | Infrastructure layer -- EF Core, repositories, external services (see `AoDaiNhaUyen.Infrastructure/AGENTS.md`) |
| `AoDaiNhaUyen.Tests/` | Unit + integration tests (see `AoDaiNhaUyen.Tests/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Clean architecture: Api -> Application -> Infrastructure; Domain standalone (no dependencies on other projects)
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
- JWT Bearer auth with Google/Facebook OAuth providers
- Token stored in HttpOnly cookies (access_token + refresh_token)
- Options pattern with ValidateDataAnnotations for config (JwtSettings, EmailSettings, GoogleOAuthSettings, FacebookOAuthSettings, CookieSettings)

## Dependencies
### External
- ASP.NET Core 10, EF Core 10, Npgsql (PostgreSQL)
- Microsoft.AspNetCore.Authentication.JwtBearer
- MailKit (SMTP email)
- DotNetEnv (.env loading)
- Google Vertex AI (virtual try-on via Gemini, stylist chat via Gemini Flash Lite)