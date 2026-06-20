<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Configuration

## Purpose
Central DI wiring for the entire backend. `ServiceRegistration.cs` exposes a single `AddBackendServices()` extension method called from `Program.cs` that registers all options, the EF `AppDbContext`, authentication schemes and authorization policies, repositories, application services, background workers, S3 storage, and HTTP clients. It is the single source of truth for what is registered and how options are validated at startup.

## Key Files
| File | Description |
|------|-------------|
| `ServiceRegistration.cs` | `AddBackendServices(IServiceCollection, IConfiguration)` extension method. Validates connection string and JWT settings eagerly; binds and validates all `IOptions<T>` with `ValidateOnStart()`; registers JWT bearer + Hermes API key auth schemes, three authorization policies (`RequireAdminRole`, `RequireCustomerRole`, `RequireAdminOrCustomer`), all repositories, all scoped/singleton services, background hosted services, S3 `IAmazonS3` singleton, and HTTP clients with `Timeout.InfiniteTimeSpan` for streaming LLM providers. |

## For AI Agents

### Working In This Directory
- Every new service, repository, or background worker must be registered here; controllers will throw at runtime otherwise.
- New `IOptions<T>` bindings should use `ValidateDataAnnotations().ValidateOnStart()` to catch misconfigurations at startup.
- The `RequireAdminRole` policy includes both `JwtBearerDefaults.AuthenticationScheme` and `HermesAdminAuthOptions.SchemeName` so Hermes can call admin endpoints without a JWT.
- S3 client is a singleton; scoped services that need it should inject `IAmazonS3` directly.

### Common Patterns
- `services.AddScoped<IFoo, FooImpl>()` for repos and services.
- `services.AddSingleton<IBar, BarImpl>()` for thread-safe in-memory stores.
- `services.AddHostedService<TWorker>()` for background workers.
- `services.AddHttpClient<IProvider, ProviderImpl>(c => c.Timeout = Timeout.InfiniteTimeSpan)` for streaming LLM providers.

## Dependencies
### Internal
- All `Application.Interfaces`, `Application.Options`, `Application.Services`
- All `Infrastructure.Repositories`, `Infrastructure.Services`, `Infrastructure.Data`
- `AoDaiNhaUyen.Api.Authentication.HermesAdminApiKeyAuthenticationHandler`
- `AoDaiNhaUyen.Api.Services.SmtpEmailService`
### External
- `Microsoft.EntityFrameworkCore` / `Npgsql`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Amazon.S3` (AWSSDK)
- `Microsoft.IdentityModel.Tokens`

<!-- MANUAL: -->
