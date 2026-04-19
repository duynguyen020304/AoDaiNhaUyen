<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen.Tests

## Purpose
Unit and integration tests for the backend. Uses xUnit test framework with EF Core InMemoryDatabase for service-level tests. Tests focus on OAuth flows, auth service, intent classification, stylist chat, and configuration validation.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Tests.csproj` | Project file -- references Api, Application, Infrastructure for testing |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Tests for options validation at startup |
| `Services/` | Tests for service implementations |

## Configuration Tests
| File | Description |
|------|-------------|
| `GoogleOAuthSettingsValidationTests.cs` | Verifies that `AddBackendServices` rejects empty GoogleOAuth:RedirectUri via OptionsValidationException |
| `FacebookOAuthSettingsValidationTests.cs` | Verifies that `AddBackendServices` rejects empty FacebookOAuth:RedirectUri via OptionsValidationException |

### Configuration Test Pattern
- Uses `ConfigurationBuilder` with `AddInMemoryCollection` to build a minimal valid configuration
- Calls `ServiceRegistration.AddBackendServices()` to register all services
- Builds a ServiceProvider and accesses the options to trigger validation
- Asserts that `OptionsValidationException` is thrown with expected message fragment

## Services Tests
| File | Description |
|------|-------------|
| `GoogleOAuthServiceTests.cs` | Tests Google OAuth code exchange: successful user retrieval and error handling with stub HTTP handler |
| `FacebookOAuthServiceTests.cs` | Tests Facebook OAuth code exchange: successful flow, bad request, and user info failure scenarios |
| `AuthServiceTests.cs` | Tests AuthService OAuth login flows: Google/Facebook exchange failures, missing email, unverified email |
| `IntentClassifierTests.cs` | Tests intent classification: Vietnamese styling requests map to correct intent, scenario, color, budget |
| `StylistChatServiceTests.cs` | Integration test: first chat turn persists thread memory with saved assistant message ID |
| `ThreadMemoryServiceTests.cs` | Tests memory state management: ApplyUserTurn extracts facts from Vietnamese text, Persist/Read round-trips structured state |

### Services Test Patterns
- **OAuth tests**: Use `StubHttpMessageHandler` with pre-configured responses to mock HTTP calls
- **AuthService tests**: Use stub implementations of all dependencies (IPasswordHasher, IJwtTokenService, IRefreshTokenService, IEmailService, OAuth services)
- **IntentClassifier tests**: Test pure Vietnamese NLP -- keyword detection for scenarios, colors, materials, budget extraction
- **StylistChatService tests**: Use InMemoryDatabase with stub collaborators to test message persistence and memory management
- **ThreadMemoryService tests**: Test pure state transformations -- fact extraction and serialization round-trips

### Test Dependencies
- All service integration tests use `AppDbContext` with InMemoryDatabase provider
- OAuth tests use `IHttpClientFactory` with stub implementations returning controlled HTTP responses
- AuthService tests create stubs as private sealed classes within the test file

## For AI Agents
### Working In This Directory
- Run `dotnet test` from this directory or from backend root
- Tests use xUnit -- `[Fact]` for individual test cases
- No shared test fixtures -- each test creates its own isolated database/context
- Stub collaborators are defined as private sealed classes at the bottom of each test file
- When adding tests for a new service, follow the pattern: stub all dependencies, test one behavior per Fact
- InMemoryDatabase is used for integration tests -- no real PostgreSQL required
- Vietnamese language testing is important for intent classification and memory service tests
