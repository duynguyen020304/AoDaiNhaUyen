<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# Entities

## Purpose
Domain classes that mirror DB schema. Plain POCOs; EF config in `Infrastructure/Data/AppDbContext.cs`.

## Entity Map
| Group | Files |
|-------|-------|
| Base/auth | `BaseEntity`, `User`, `UserAccount`, `Role`, `UserRole`, `UserSession`, `EmailVerificationToken`, `PasswordResetToken` |
| Profile/address | `UserAddress`, `MeasurementProfile` |
| Catalog | `Category`, `Product`, `ProductVariant`, `ProductImage` |
| Style/AI catalog | `StyleScenario`, `ProductStyleProfile`, `ProductScenario`, `ProductPairing`, `ProductAiAsset` |
| Chat/AI output | `ChatThread`, `ChatMessage`, `ChatAttachment`, `ChatThreadMemory`, `ImageValidationCacheEntry`, `UserGeneratedImage` |
| Cart/order | `Cart`, `CartItem`, `Order`, `OrderItem`, `Payment`, `Shipment` |
| Promo/reputation | `PromoCode`, `OrderPromoCode`, `OrderPromoCostSnapshot`, `Review`, `Comment` |
| Email/marketing | `EmailTemplate`, `EmailJob`, `EmailSendLog`, `Subscriber`, `MarketingConsent`, `CustomerEvent`, `OrderAttribution` |
| Blog | `BlogCategory`, `BlogPost`, `BlogImage` |
| Admin AI/audit | `AdminAiAction`, `ToolRiskConfig`, `LlmAuditLog` |

## Relationship Notes
- `Product` owns variants/images/style profiles/scenarios/pairings/AI assets.
- `ChatThread` supports either authenticated `UserId` or anonymous `GuestKeyHash`.
- `ChatThreadMemory` is one-to-one thread state for extracted facts and resolved product refs.
- `Cart` is one per user; `CartItem` unique by `(CartId, VariantId)`.
- `Order` snapshots shipping address and totals; `OrderItem` snapshots product/variant data.
- Promo costs can be snapshotted via `OrderPromoCostSnapshot` to preserve attribution/cost history.
- Marketing/email entities connect subscribers, consent, jobs, sends, customer events, and order attribution.

## Local Conventions
- Most IDs are `long`; `Role.Id` is `short`; joins may use composite PKs.
- Navigation collections use `= new List<T>()`.
- Non-null nav refs use `null!`; no EF package refs here.
- JSON columns are string properties ending in `Jsonb`/`Json` and mapped in `AppDbContext`.
- Adding entity requires DbSet + mapping + migration + service/repo registration as needed.
