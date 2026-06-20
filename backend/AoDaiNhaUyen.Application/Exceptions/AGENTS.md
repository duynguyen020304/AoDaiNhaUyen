<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Exceptions

## Purpose
Domain-level custom exception types thrown by Application and Infrastructure services. Each exception represents a specific failure mode in a subsystem (AI try-on, OAuth, image validation) and is caught by API-layer middleware or controller error handling to produce structured error responses.

## Key Files
| File | Description |
|------|-------------|
| `AiTryOnConfigurationException.cs` | Thrown when AI try-on provider is misconfigured (missing credentials, invalid endpoint) |
| `AiTryOnProviderException.cs` | Thrown when the AI try-on provider (Vertex AI) returns an error or unexpected response |
| `GoogleOAuthExchangeException.cs` | Thrown when Google authorization code exchange fails |
| `ZaloOAuthExchangeException.cs` | Thrown when Zalo authorization code exchange fails |
| `ImageValidationConfigurationException.cs` | Thrown when image validation options are missing or invalid on startup |
| `ImageValidationProviderException.cs` | Thrown when the image validation provider (AI backend) returns an error |

## For AI Agents

### Working In This Directory
- All exceptions are `sealed` and extend `Exception`
- Use primary constructor syntax (`sealed class Foo(string message) : Exception(message)`) for single-message exceptions
- These are caught in the API layer (exception filters / middleware) — check `AoDaiNhaUyen.Api` for handler mappings
- Configuration exceptions (`*ConfigurationException`) are typically thrown at startup or first use and indicate a deployment error
- Provider exceptions (`*ProviderException`) are runtime failures from external services and should be handled gracefully

### Common Patterns
```csharp
// Single-message primary constructor (preferred):
public sealed class AiTryOnConfigurationException(string message) : Exception(message);

// Multi-property variant:
public sealed class GoogleOAuthExchangeException : Exception
{
  public GoogleOAuthExchangeException(string message) : base(message) { }
}
```

## Dependencies
### Internal
- Thrown by: `AoDaiNhaUyen.Infrastructure` service implementations
- Caught by: `AoDaiNhaUyen.Api` exception middleware/filters

<!-- MANUAL: -->
