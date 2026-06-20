<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# AoDaiNhaUyen.Domain

## Purpose
Pure domain layer: plain C# POCOs for all entities, enums, base classes, domain constants, and static seed data. No EF Core, ASP.NET, or infrastructure dependencies. EF mapping, migrations, and persistence all live in Infrastructure.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Domain.csproj` | Project file — no EF or framework references |
| `Common/BaseEntity.cs` | Abstract base: `Guid Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `IsActive`, `DeletedAt` |
| `Common/ISoftDeletable.cs` | Interface requiring `IsDeleted` + `DeletedAt` |
| `Common/RiskLevel.cs` | Enum: Read/Low/Medium/High/Critical for AI agent action safety |
| `Common/AdminAiActionType.cs` | Enum: Query/Create/Update/Delete/Restore/Toggle/RoleChange/ImageUpload/Generative/Chat |
| `Common/BlogPostStatus.cs` | Enum: Draft/Published/Archived |
| `Common/BlogPostTemplate.cs` | Enum: StandardArticle/PhotoGallery/VideoFeature/ProductSpotlight/HowTo |
| `Common/ChatSources.cs` | Constants: `web`, `admin_ai` chat source strings |
| `Constants/RoleNames.cs` | Constants: `admin`, `customer` role name strings |
| `Entities/` | All domain entity classes (see `Entities/AGENTS.md`) |
| `SeedData/` | Static seed data for roles, users, categories, products, materials, store locations |

## Subdirectory Summary
| Directory | Purpose |
|-----------|---------|
| `Common/` | Base classes, interfaces, enums, shared constants used across entities |
| `Constants/` | Application-wide string/value constants (role names, etc.) |
| `Entities/` | One file per entity; mirrors DB schema; no EF attributes |
| `SeedData/` | Static default data loaded by `SeedDataService` at startup |

## Entity Groups
| Group | Entities |
|-------|----------|
| Auth/user | `User`, `UserAccount`, `UserRole`, `Role`, `UserSession`, `EmailVerificationToken`, `PasswordResetToken`, `UserAddress`, `MeasurementProfile` |
| Catalog | `Category`, `Product`, `ProductVariant`, `ProductImage` |
| AI/style/chat | `ProductAiAsset`, `ProductStyleProfile`, `ProductScenario`, `ProductPairing`, `StyleScenario`, `ChatThread`, `ChatMessage`, `ChatAttachment`, `ChatThreadMemory`, `ImageValidationCacheEntry`, `UserGeneratedImage` |
| Cart/order/payment | `Cart`, `CartItem`, `Order`, `OrderItem`, `Payment`, `Shipment` |
| Promo/reviews/comments | `PromoCode`, `OrderPromoCode`, `OrderPromoCostSnapshot`, `Review`, `Comment` |
| Marketing/email/events | `EmailTemplate`, `EmailJob`, `EmailSendLog`, `Subscriber`, `MarketingConsent`, `CustomerEvent`, `OrderAttribution` |
| Blog | `BlogCategory`, `BlogPost`, `BlogImage` |
| Admin AI/audit | `AdminAiAction`, `ToolRiskConfig`, `LlmAuditLog` |
| Hermes agent | `HermesRun`, `HermesHeartbeat`, `HermesAgentTraceStep`, `HermesReport`, `HermesEventOutbox`, `HermesMonitorLink` |
| AI try-on feedback | `AiTryOnFeedback` |

## For AI Agents
### Working In This Directory
- Add new entity: create `.cs` file here, then add `DbSet` + mapping in `AppDbContext`, create migration, register service/repo as needed.
- Keep domain classes free of EF, ASP.NET, and infrastructure package references.
- New enums or shared types belong in `Common/`; role/status string constants belong in `Constants/`.
- New seed data static classes belong in `SeedData/`.

### Common Patterns
- Most IDs are `Guid` (migrated from `long` in `InitGuid` migration). `Role.Id` is `short`.
- Navigation collections initialized as `= new List<T>()`.
- Non-null nav references use `null!` sentinel.
- JSON/JSONB columns are `string` properties ending in `Jsonb` or `Json`; EF maps them as `jsonb` in Infrastructure.
- Product type values: `ao_dai`, `phu_kien`.

## Dependencies
### Internal
- Referenced by `AoDaiNhaUyen.Application` and `AoDaiNhaUyen.Infrastructure`
### External
- None (pure C# — no NuGet dependencies)

<!-- MANUAL: -->
