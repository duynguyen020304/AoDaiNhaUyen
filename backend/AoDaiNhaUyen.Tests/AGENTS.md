<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# AoDaiNhaUyen.Tests

## Purpose
xUnit backend tests. Project is separate from `AoDaiNhaUyen.slnx`; run explicitly from this directory.

## Test Areas
| Directory | Coverage |
|-----------|----------|
| `Configuration/` | OAuth/options/config validation |
| `Controllers/` | Controller behavior such as chat endpoints |
| `Services/` | Auth/OAuth, AI chat/try-on helpers, cache, email, S3/storage paths, promo cost, order attribution, admin AI security |

## Common Patterns
- Uses xUnit + `Microsoft.NET.Test.Sdk`.
- Service tests often use inline private stub classes inside each test file.
- EF-related tests use InMemoryDatabase where needed.
- Tests focus service behavior and edge cases more than full HTTP integration.

## Commands
- `dotnet test` from `backend/AoDaiNhaUyen.Tests/`
- For whole backend build first: `cd .. && dotnet build`

## Gotchas
- Running `dotnet test` against `backend/AoDaiNhaUyen.slnx` will not include this test project.
- Keep fake secrets/config obviously fake; never copy real `.env` values.
- Add tests near touched service/controller when changing auth, AI, storage, promo, email, cache, or order logic.
- Prefer narrow service tests over broad setup-heavy integration tests unless HTTP/auth behavior matters.
