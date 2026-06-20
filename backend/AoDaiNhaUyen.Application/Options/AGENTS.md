<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Options

## Purpose
Strongly-typed configuration classes bound from `appsettings.json` (or environment variables) via the ASP.NET Options pattern. All classes are `sealed` with `DataAnnotations` for startup validation. Registered and validated in `AoDaiNhaUyen.Api` or `AoDaiNhaUyen.Infrastructure` via `AddOptions<T>().BindConfiguration(section).ValidateDataAnnotations().ValidateOnStart()`.

## Key Files
| File | Class | Section | Purpose |
|------|-------|---------|---------|
| `JwtSettings.cs` | `JwtSettings` | `Jwt` | `SecretKey`, `Issuer`, `Audience`, `ExpiryMinutes` (60), `RefreshTokenExpiryDays` (30) |
| `CookieSettings.cs` | `CookieSettings` | _(direct)_ | `AccessTokenCookieName`, `RefreshTokenCookieName` cookie names |
| `EmailSettings.cs` | `EmailSettings` | `Email` | SMTP host/port/credentials, `EnableSsl`, `FromEmail`, `FromName`, `ApiBaseUrl`, `FrontendBaseUrl` |
| `GoogleOAuthSettings.cs` | `GoogleOAuthSettings` | `GoogleOAuth` | `ClientId`, `ClientSecret`, `RedirectUri` |
| `ZaloOAuthSettings.cs` | `ZaloOAuthSettings` | `ZaloOAuth` | `AppId`, `SecretKey`, `RedirectUri` |
| `AdminSeedOptions.cs` | `AdminSeedOptions` | `AdminSeed` | Bootstrap admin `Email` / `Password` — used only by `ISeedDataService` |
| `AiTryOnConcurrencyOptions.cs` | `AiTryOnConcurrencyOptions` | `AiTryOnConcurrency` | `MaxConcurrentGenerations` (default 3) — controls `SemaphoreSlim` in try-on service |
| `ChatConcurrencyOptions.cs` | `ChatConcurrencyOptions` | `ChatConcurrency` | `MaxConcurrentThreads` (default 10) — controls `SemaphoreSlim` in stylist chat service |
| `HermesAgentOptions.cs` | `HermesAgentOptions` | `Hermes` | `ApiServerUrl`, `ApiServerKey`, `RunnerName` — Hermes admin agent connection |
| `HermesOutboxOptions.cs` | `HermesOutboxOptions` | `HermesOutbox` | `Enabled`, `DryRun`, `BatchSize`, `PollIntervalSeconds`, `MaxAttempts`, `LockTimeoutMinutes`, `MaxPayloadBytes`, `HighValueOrderThreshold`, `LowStockThreshold`, `EventPath` |
| `ImageValidationOptions.cs` | `ImageValidationOptions` | `ImageValidation` | `CacheTtlDays`, `MaxImageBytes`, `MinWidth/Height`, `MaxWidth/Height`, `AllowedExtensions` |

## For AI Agents

### Working In This Directory
- All options classes are `sealed` — do not inherit from them
- Use `public const string SectionName = "..."` when the section name needs to be referenced in registration code
- Properties use either `{ get; set; }` (mutable, for options that can be reloaded) or `{ get; init; }` (immutable after construction)
- `[Required]` + `[Range]` + `[EmailAddress]` annotations from `System.ComponentModel.DataAnnotations` drive startup validation
- Sensitive fields (`SecretKey`, `Password`, `ClientSecret`) should never be logged

### Common Patterns
```csharp
// Registration in Program.cs / ServiceRegistration:
services.AddOptions<JwtSettings>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Consuming in a service:
public MyService(IOptions<JwtSettings> jwtOptions)
{
    _jwt = jwtOptions.Value;
}
```

## Dependencies
### Internal
- Consumed by: Infrastructure service implementations via `IOptions<T>` / `IOptionsMonitor<T>`
- Registration: `AoDaiNhaUyen.Api` startup or `AoDaiNhaUyen.Infrastructure/ServiceRegistration.cs`

### External
- `System.ComponentModel.DataAnnotations` — validation attributes
- `Microsoft.Extensions.Options` — options pattern interfaces

<!-- MANUAL: -->
