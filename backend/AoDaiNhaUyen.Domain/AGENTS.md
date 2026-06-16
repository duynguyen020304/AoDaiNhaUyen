<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# AoDaiNhaUyen.Domain

## Purpose
Domain entity classes and static seed data. Standalone project with plain C# classes; EF mapping lives in Infrastructure.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Entities/` | Entity classes (see `Entities/AGENTS.md`) |
| `SeedData/` | Static default roles/customers/categories/products/materials/store locations |

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

## Seed Data
| File | Description |
|------|-------------|
| `DefaultRoles.cs` | Role names/ids |
| `DefaultCustomers.cs` | Seed customer/admin accounts |
| `DefaultCategories.cs` | Category tree |
| `DefaultProducts.cs` | Product catalog, variants, images |
| `DefaultMaterials.cs` | Material mappings |
| `DefaultStoreLocations.cs` | Store location data |

## Local Conventions
- Main entities use `long Id`; `Role` uses `short`; some join tables use composite keys.
- Navigation collections initialized as empty lists.
- No EF Core refs/attributes in Domain; configure tables/constraints/indexes in `AppDbContext`.
- JSON-ish data stored as string `...Jsonb` and mapped as PostgreSQL `jsonb` in Infrastructure.
- Product type values: `ao_dai`, `phu_kien`.
- New entity flow: class here -> DbSet + mapping in `AppDbContext` -> migration -> service/repo updates.
