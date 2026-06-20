<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Controllers

## Purpose
xUnit tests for ASP.NET Core controller behavior. Focuses on request routing, response shaping, error handling, and authorization logic without requiring a full HTTP test server unless the test specifically targets middleware behavior.

## Key Files
| File | Description |
|------|-------------|
| `ChatControllerTests.cs` | Tests for the AI stylist chat controller endpoints (message flow, error cases, concurrency) |

## For AI Agents

### Working In This Directory
- Add a `*ControllerTests.cs` here whenever a new controller or a new action method is added.
- Prefer unit-testing controller methods directly by constructing the controller with mocked/stubbed services over full WebApplicationFactory integration tests unless auth middleware behavior needs to be verified.
- Run from `backend/AoDaiNhaUyen.Tests/`: `dotnet test --filter "FullyQualifiedName~Controllers"`

### Common Patterns
- Construct the controller under test manually, injecting stub service implementations.
- Assert on `ObjectResult.Value` cast to `ApiResponse<T>` to verify the envelope shape.
- Use inline private stub classes for service dependencies rather than a mocking framework.
- Test error paths (service throws, unauthorized, bad input) alongside the happy path.

## Dependencies
### Internal
- `AoDaiNhaUyen.Api` (controllers)
- `AoDaiNhaUyen.Application` (interfaces, DTOs)

### External
- xUnit
