<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen.Tests

## Purpose
Backend unit + integration tests. Uses xUnit with EF Core InMemoryDatabase for service-level tests. Tests cover OAuth flows, auth service, intent classification, stylist chat, config validation.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Tests.csproj` | Project file -- references Api, Application, Infrastructure for testing |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Startup options validation tests |
| `Services/` | Service implementation tests |

## Configuration Tests
| File | Description |
|------|-------------|
| `GoogleOAuthSettingsValidationTests.cs` | Verifies `AddBackendServices` rejects empty GoogleOAuth:RedirectUri via OptionsValidationException |
| `FacebookOAuthSettingsValidationTests.cs` | Verifies `AddBackendServices` rejects empty FacebookOAuth:RedirectUri via OptionsValidationException |

### Configuration Test Pattern
- Uses `ConfigurationBuilder` with `AddInMemoryCollection` to build minimal valid config
- Calls `ServiceRegistration.AddBackendServices()` to register all services
- Builds ServiceProvider and accesses options to trigger validation
- Asserts `OptionsValidationException` thrown with expected message fragment

## Services Tests
| File | Description |
|------|-------------|
| `GoogleOAuthServiceTests.cs` | Tests Google OAuth code exchange: user retrieval success + error handling with stub HTTP handler |
| `FacebookOAuthServiceTests.cs` | Tests Facebook OAuth code exchange: success flow, bad request, user info failure |
| `AuthServiceTests.cs` | Tests AuthService OAuth login flows: Google/Facebook exchange failures, missing email, unverified email |
| `IntentClassifierTests.cs` | Tests intent classification: Vietnamese styling requests map to correct intent, scenario, color, budget |
| `StylistChatServiceTests.cs` | Integration test: first chat turn persists thread memory with saved assistant message ID |
| `ThreadMemoryServiceTests.cs` | Tests memory state management: ApplyUserTurn extracts facts from Vietnamese text, Persist/Read round-trips structured state |

### Services Test Patterns
- **OAuth tests**: Use `StubHttpMessageHandler` with pre-configured responses to mock HTTP calls
- **AuthService tests**: Use stub implementations of all dependencies (IPasswordHasher, IJwtTokenService, IRefreshTokenService, IEmailService, OAuth services)
- **IntentClassifier tests**: Test pure Vietnamese NLP -- keyword detection for scenarios, colors, materials, budget extraction
- **StylistChatService tests**: Use InMemoryDatabase with stub collaborators to test message persistence + memory management
- **ThreadMemoryService tests**: Test pure state transforms -- fact extraction + serialization round-trips

### Test Dependencies
- All service integration tests use `AppDbContext` with InMemoryDatabase provider
- OAuth tests use `IHttpClientFactory` with stub implementations returning controlled HTTP responses
- AuthService tests create stubs as private sealed classes within test file

## For AI Agents
### Working In This Directory
- Run `dotnet test` from this directory or backend root
- Tests use xUnit -- `[Fact]` for individual test cases
- No shared test fixtures -- each test creates own isolated database/context
- Stub collaborators defined as private sealed classes at bottom of each test file
- When adding tests for new service, follow pattern: stub all dependencies, test one behavior per Fact
- InMemoryDatabase used for integration tests -- no real PostgreSQL required
- Vietnamese language testing important for intent classification + memory service tests