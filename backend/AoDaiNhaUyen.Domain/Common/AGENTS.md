<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Common

## Purpose
Shared base classes, interfaces, enums, and constants used across all domain entities. Provides the soft-delete contract (`ISoftDeletable`), the auditable base (`BaseEntity`), and all domain-level enums needed by entities and services.

## Key Files
| File | Description |
|------|-------------|
| `BaseEntity.cs` | Abstract base with `Guid Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `IsActive`, `DeletedAt`; implements `ISoftDeletable` |
| `ISoftDeletable.cs` | Interface: `bool IsDeleted`, `DateTime? DeletedAt` — implemented by `BaseEntity` and any entity needing soft-delete without full base |
| `AdminAiActionType.cs` | Enum classifying admin AI actions: Query/Create/Update/Delete/Restore/Toggle/RoleChange/ImageUpload/Generative/Chat |
| `RiskLevel.cs` | Enum for AI agent safety gate: Read(0)/Low(1)/Medium(2)/High(3)/Critical(4) |
| `BlogPostStatus.cs` | Enum with `[JsonStringEnumConverter]`: Draft/Published/Archived |
| `BlogPostTemplate.cs` | Enum with `[JsonStringEnumConverter]`: StandardArticle/PhotoGallery/VideoFeature/ProductSpotlight/HowTo |
| `ChatSources.cs` | Static class with `const string Web = "web"` and `AdminAi = "admin_ai"` |

## For AI Agents
### Working In This Directory
- Place new enums or shared interfaces here when they are needed by multiple entities or services.
- Do not add EF, ASP.NET, or infrastructure references.
- Enums serialized to JSON (e.g., in API responses) should carry `[JsonConverter(typeof(JsonStringEnumConverter))]`.
- `BaseEntity` uses `Guid Id` — do not revert to `long`; all new entities should inherit `BaseEntity` unless they have a special PK (e.g., `Role` uses `short Id`).

### Common Patterns
- `BaseEntity` is the standard base for all main entities.
- Entities needing soft-delete without inheriting `BaseEntity` implement `ISoftDeletable` directly.
- `RiskLevel` is used by `ToolRiskConfig` and `SafetyGate` to control AI action approval flow.
- `AdminAiActionType` is recorded in `AdminAiAction` for audit logging of every admin AI operation.

## Dependencies
### Internal
- Used by all entities in `../Entities/`
- `RiskLevel` and `AdminAiActionType` referenced in `Infrastructure/Services/` (safety gate, audit)
### External
- `System.Text.Json.Serialization` (for enum converters only)

<!-- MANUAL: -->
