<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# AoDaiNhaUyen.Tests

## Purpose
xUnit backend test project. Covers service logic, configuration validation, controller behavior, and edge cases for auth, AI, storage, cache, promo, email, and order flows. Not included in `AoDaiNhaUyen.slnx`; must be run explicitly from this directory.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Tests.csproj` | Test project file referencing xUnit + Microsoft.NET.Test.Sdk |

## For AI Agents

### Working In This Directory
- Run `dotnet test` from `backend/AoDaiNhaUyen.Tests/` — running from `backend/` via the `.slnx` will skip this project.
- For whole backend build first: `cd .. && dotnet build`
- Keep fake secrets/config obviously fake; never copy real `.env` values.
- Add tests near touched service/controller when changing auth, AI, storage, promo, email, cache, or order logic.
- Prefer narrow service tests over broad setup-heavy integration tests unless HTTP/auth behavior matters.

### Testing Requirements
- Test runner: xUnit
- New service code should have a corresponding `*Tests.cs` in `Services/`
- New controller code should have a corresponding `*Tests.cs` in `Controllers/`
- New config/options validation should have a corresponding `*Tests.cs` in `Configuration/`
- EF-related tests use InMemoryDatabase where needed
- Service tests often use inline private stub classes inside each test file

### Common Patterns
- Inline stub/fake implementations within each test class file (no shared mock infrastructure)
- InMemoryDatabase for EF-dependent tests
- Focus on service behavior and edge cases over full HTTP integration
- Keep test data obviously synthetic (fake credentials, placeholder URLs)

## Test Areas
| Directory | Coverage |
|-----------|----------|
| `Configuration/` | Options validation for GoogleOAuth and ZaloOAuth settings |
| `Controllers/` | Controller behavior — chat endpoints |
| `Services/` | Auth/OAuth, AI chat/try-on, cache invalidation, image validation, catalog styling/try-on, concurrency limits, customer events, email templates, order attribution, promo cost/concurrency, seed data, stylist chat/fallback, subscriber, thread memory, upload storage paths, Vertex AI responses, Zalo OAuth |

## Dependencies
### Internal
- `AoDaiNhaUyen.Application`, `AoDaiNhaUyen.Domain`, `AoDaiNhaUyen.Infrastructure`

### External
- xUnit, Microsoft.NET.Test.Sdk
- Microsoft.EntityFrameworkCore.InMemory (for EF-dependent tests)
