<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Services

## Purpose
API-host-layer service implementations — concrete service classes that belong in the API project rather than the Application or Infrastructure layers. Currently contains a single SMTP email service. Registered in `ServiceRegistration.cs` as `IEmailService`.

## Key Files
| File | Description |
|------|-------------|
| `SmtpEmailService.cs` | Implements `IEmailService` using MailKit. Connects to the configured SMTP host with optional StartTLS, authenticates, sends an HTML email, then disconnects. Throws `InvalidOperationException` on failure (wrapping the inner exception). Config comes from `EmailSettings` options. |

## For AI Agents

### Working In This Directory
- New API-host services (those that cannot live in `Application` or `Infrastructure`) go here.
- `SmtpEmailService` strips spaces from the SMTP password before authenticating — this is intentional for copy-paste app passwords.
- Do not inject `HttpContext` or controller concerns into services here; they must remain independently testable.

### Common Patterns
- Primary constructor injection of `IOptions<T>` resolved via `.Value` into a private field.
- Async methods with `CancellationToken` passed through to all I/O calls.
- Wrap infrastructure exceptions in a domain-meaningful `InvalidOperationException`.

## Dependencies
### Internal
- `AoDaiNhaUyen.Application.Interfaces.Services.IEmailService` — interface implemented
- `AoDaiNhaUyen.Application.Options.EmailSettings` — SMTP configuration
### External
- `MailKit` / `MimeKit`

<!-- MANUAL: -->
