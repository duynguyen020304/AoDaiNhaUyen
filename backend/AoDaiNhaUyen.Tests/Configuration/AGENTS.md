<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Configuration

## Purpose
xUnit tests for Options-pattern configuration classes. Validates that required settings are enforced, defaults are applied correctly, and misconfigured options fail at startup rather than silently at runtime.

## Key Files
| File | Description |
|------|-------------|
| `GoogleOAuthSettingsValidationTests.cs` | Validates GoogleOAuth options (ClientId, ClientSecret, RedirectUri required; format checks) |
| `ZaloOAuthSettingsValidationTests.cs` | Validates ZaloOAuth options (AppId, SecretKey, RedirectUri required; format checks) |

## For AI Agents

### Working In This Directory
- Add a new `*ValidationTests.cs` file here whenever a new Options class with `[Required]` or custom validation is added to the backend.
- Test both the valid path and each individual missing/invalid field to confirm `ValidateDataAnnotations()` / `Validate()` fail correctly.
- Run from `backend/AoDaiNhaUyen.Tests/`: `dotnet test --filter "FullyQualifiedName~Configuration"`

### Common Patterns
- Instantiate the options class directly, set properties, then call the validator.
- Use `DataAnnotationsValidator` or custom `IValidateOptions<T>` depending on what the production code registers.
- One test class per options class; name it `<OptionsClassName>ValidationTests`.

## Dependencies
### Internal
- `AoDaiNhaUyen.Application` (options definitions)

### External
- xUnit
