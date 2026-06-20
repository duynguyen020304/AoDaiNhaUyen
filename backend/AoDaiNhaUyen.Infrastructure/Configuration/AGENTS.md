<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Configuration

## Purpose
Strongly-typed options POCOs for external service configuration. These classes are bound from `appsettings.json` / environment variables via `IOptions<T>` in the API project. No logic — pure data containers for infrastructure settings.

## Key Files
| File | Description |
|------|-------------|
| `GoogleCloudOptions.cs` | Vertex AI / Google Cloud settings: `ApiKey`, `ProjectId`, `Location`, model names for try-on/stylist/validation, timeout values |
| `S3StorageSettings.cs` | S3-compatible storage settings: `BucketName`, `Region`, `AccessKey`, `SecretKey`, `ServiceUrl`, `UsePathStyle` (supports AWS S3 and MinIO) |

## For AI Agents
### Working In This Directory
- Add new options classes here when a new external service requires configuration (e.g., SMTP, payment gateway, new AI provider).
- Keep classes as plain POCOs: public properties with defaults, `const string SectionName` for the config section key.
- Register binding in `Api/Configuration/ServiceRegistration.cs` via `services.Configure<TOptions>(config.GetSection(TOptions.SectionName))`.
- Never instantiate these directly in services — inject `IOptions<T>` or `IOptionsMonitor<T>`.

### Common Patterns
- `S3StorageSettings` uses `public const string SectionName = "S3Storage"` to identify the appsettings section.
- `GoogleCloudOptions` uses sensible defaults (location, model names) so missing config does not crash startup.
- `UsePathStyle = true` on `S3StorageSettings` enables MinIO / self-hosted S3 compatibility.

## Dependencies
### Internal
- Consumed by `Services/S3StorageService.cs`, `Services/VertexAi*.cs`, and other external-API services
### External
- None (plain C# — no package dependencies)

<!-- MANUAL: -->
