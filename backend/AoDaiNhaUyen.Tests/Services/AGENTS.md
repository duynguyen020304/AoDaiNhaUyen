<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Services

## Purpose
xUnit tests for backend service logic — the largest test suite in the project. Covers auth/OAuth, AI try-on/stylist chat, cache invalidation, image validation, catalog operations, concurrency limiting, customer events, email templates, order attribution, promo cost, seed data, subscriber management, thread memory, upload path resolution, and Vertex AI responses.

## Key Files
| File | Description |
|------|-------------|
| `AdminAiSecurityTests.cs` | Admin AI security guardrails and prompt injection resistance |
| `AuthServiceTests.cs` | Credential auth, token issuance, refresh, logout flows |
| `CacheInvalidationServiceTests.cs` | Cache key invalidation on entity change |
| `CachedImageValidationServiceTests.cs` | Image validation with caching layer |
| `CatalogStylingServiceTests.cs` | Catalog product styling logic |
| `CatalogTryOnServiceTests.cs` | Catalog-level AI try-on orchestration |
| `ConcurrencyLimitedAiTryOnServiceTests.cs` | Semaphore/concurrency limiting for try-on requests |
| `ConcurrencyLimitedStylistChatServiceTests.cs` | Semaphore/concurrency limiting for stylist chat |
| `CustomerEventServiceTests.cs` | Customer event recording and attribution |
| `EmailTemplateServiceTests.cs` | Email template rendering and variable substitution |
| `GoogleOAuthServiceTests.cs` | Google OAuth token exchange and user info fetch |
| `ImageVisibilityServiceTests.cs` | Image visibility rules (public/private/owner-only) |
| `IntentClassifierTests.cs` | AI intent classification for chat routing |
| `OrderAttributionServiceTests.cs` | Order attribution to sessions/campaigns |
| `PromoConcurrencyTests.cs` | Promo code concurrent claim handling |
| `PromoCostServiceTests.cs` | Promo discount calculation logic |
| `SeedDataServiceTests.cs` | DB seed data idempotency and content |
| `StylistChatServiceTests.cs` | Full stylist chat service behavior |
| `StylistFallbackTextServiceTests.cs` | Fallback responses when AI is unavailable |
| `SubscriberServiceTests.cs` | Email subscriber subscribe/unsubscribe flows |
| `ThreadMemoryServiceTests.cs` | Chat thread message history and memory management |
| `UploadStoragePathResolverTests.cs` | S3/upload path construction for different asset types |
| `VertexAiImageValidationServiceTests.cs` | Vertex AI image safety/validation responses |
| `VertexAiStylistResponseComposerTests.cs` | Vertex AI stylist response assembly and streaming |
| `VertexAiTryOnServiceTests.cs` | Vertex AI virtual try-on API integration |
| `ZaloOAuthServiceTests.cs` | Zalo OAuth token exchange and user info fetch |

## For AI Agents

### Working In This Directory
- Add a `*Tests.cs` here whenever a new service is added to `AoDaiNhaUyen.Infrastructure/Services/` or `AoDaiNhaUyen.Application/Services/`.
- When modifying an existing service, check if a corresponding test file exists and update it.
- Run from `backend/AoDaiNhaUyen.Tests/`: `dotnet test --filter "FullyQualifiedName~Services"`

### Testing Requirements
- Use inline private stub/fake classes inside the test class rather than a mocking framework.
- For services that depend on EF DbContext, use `Microsoft.EntityFrameworkCore.InMemory`.
- Test edge cases: null inputs, service throws, empty results, concurrent access where relevant.
- AI service tests should verify prompt construction and response parsing, not live API calls (stub the HTTP client or Vertex AI client).

### Common Patterns
- One test class per service class; name it `<ServiceClassName>Tests`.
- Arrange/Act/Assert structure with descriptive `[Fact]` / `[Theory]` names.
- Inline stubs implement the minimum interface surface needed for the test scenario.
- Concurrency tests use `Task.WhenAll` with controlled delays in stubs to trigger race conditions.

## Dependencies
### Internal
- `AoDaiNhaUyen.Infrastructure` (service implementations)
- `AoDaiNhaUyen.Application` (interfaces, DTOs)
- `AoDaiNhaUyen.Domain` (entities, seed data)

### External
- xUnit
- Microsoft.EntityFrameworkCore.InMemory
