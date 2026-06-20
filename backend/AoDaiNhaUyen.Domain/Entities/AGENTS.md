<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Entities

## Purpose
Domain entity classes that mirror the database schema. Plain POCOs only — no EF Core attributes, no business logic, no infrastructure references. All EF configuration (table names, indexes, constraints, jsonb mappings) lives in `Infrastructure/Data/AppDbContext.cs`.

## Key Files
| File | Description |
|------|-------------|
| `User.cs` | Core user: email, phone, display name, status, avatar; links to `UserAccount`, `UserRole`, `UserSession` |
| `UserAccount.cs` | Credentials row: local password hash or OAuth provider token per user |
| `UserRole.cs` | Join table: `(UserId, RoleId)` |
| `Role.cs` | Role with `short Id`; name string matches `RoleNames` constants |
| `UserSession.cs` | Refresh-token session with IP (`inet`), device info, expiry |
| `EmailVerificationToken.cs` | One-time email verification token |
| `PasswordResetToken.cs` | One-time password reset token |
| `UserAddress.cs` | Saved delivery addresses per user |
| `MeasurementProfile.cs` | Body measurements for custom ao dai sizing |
| `Category.cs` | Product category with slug and parent ref |
| `Product.cs` | Main product: type (`ao_dai`/`phu_kien`), slug, price, stock, public/active flags |
| `ProductVariant.cs` | SKU-level variant: size, color, price, stock |
| `ProductImage.cs` | Product image URLs with display order and visibility flag |
| `ProductAiAsset.cs` | Curated AI try-on image assets per product |
| `ProductStyleProfile.cs` | Style keywords/formality/color family for AI stylist (jsonb) |
| `ProductScenario.cs` | Occasion scenario scores for a product |
| `ProductPairing.cs` | Product-to-product pairing suggestion |
| `StyleScenario.cs` | Named style scenarios (e.g., wedding, office) |
| `ChatThread.cs` | Chat session: optional `UserId` or anonymous `GuestKeyHash`; source (`web`/`admin_ai`) |
| `ChatMessage.cs` | Individual message with role, content, usage jsonb, tool calls jsonb |
| `ChatAttachment.cs` | Image/file attachment on a chat message (metadata jsonb) |
| `ChatThreadMemory.cs` | One-to-one thread state: extracted facts and resolved product refs (jsonb) |
| `ImageValidationCacheEntry.cs` | Cache for Vertex AI image validation results |
| `UserGeneratedImage.cs` | AI try-on output images stored per user |
| `Cart.cs` | One cart per user |
| `CartItem.cs` | Unique by `(CartId, VariantId)`; quantity > 0 |
| `Order.cs` | Snapshot of shipping address, totals, status; unique `OrderCode` |
| `OrderItem.cs` | Snapshot of product/variant data at order time; custom measurements jsonb |
| `Payment.cs` | Payment record linked to order |
| `Shipment.cs` | Shipment tracking per order |
| `PromoCode.cs` | Discount codes with usage limits, validity window, amount/percent |
| `OrderPromoCode.cs` | Applied promo codes on an order |
| `OrderPromoCostSnapshot.cs` | Cost/attribution snapshot for promo at order time |
| `Review.cs` | Product review with rating (1–5) and content |
| `Comment.cs` | Product comment with optional rating |
| `EmailTemplate.cs` | Named email templates with subject/body |
| `EmailJob.cs` | Queued email job |
| `EmailSendLog.cs` | Record of sent emails |
| `Subscriber.cs` | Newsletter subscriber |
| `MarketingConsent.cs` | Per-user marketing consent record |
| `CustomerEvent.cs` | Behavioural events (view, add-to-cart, purchase, etc.) for marketing automation |
| `OrderAttribution.cs` | Marketing channel attribution for an order |
| `BlogCategory.cs` | Blog category with slug |
| `BlogPost.cs` | Blog post with status (`BlogPostStatus`), template (`BlogPostTemplate`), SEO fields |
| `BlogImage.cs` | Images attached to a blog post |
| `AdminAiAction.cs` | Audit record of every admin AI action: type, risk, payload, result |
| `ToolRiskConfig.cs` | Per-tool risk level override config for the safety gate |
| `LlmAuditLog.cs` | Raw LLM prompt/response audit log |
| `HermesRun.cs` | Hermes agent run record: status, goal, output |
| `HermesHeartbeat.cs` | Periodic heartbeat/progress update for a Hermes run |
| `HermesAgentTraceStep.cs` | Individual tool-call step trace within a Hermes run |
| `HermesReport.cs` | Final report produced by a Hermes agent run |
| `HermesEventOutbox.cs` | Outbox table for reliable Hermes event delivery |
| `HermesMonitorLink.cs` | External monitor link for a Hermes run (share URL) |
| `AiTryOnFeedback.cs` | User feedback on AI try-on result images |

## Relationship Notes
- `Product` owns variants, images, style profiles, scenarios, pairings, AI assets.
- `ChatThread` supports either authenticated `UserId` or anonymous `GuestKeyHash`.
- `ChatThreadMemory` is one-to-one thread state for extracted facts and resolved product refs.
- `Cart` is one per user; `CartItem` unique by `(CartId, VariantId)`.
- `Order` snapshots shipping address and totals; `OrderItem` snapshots product/variant data.
- Promo costs snapshotted via `OrderPromoCostSnapshot` to preserve attribution/cost history.
- Hermes entities track full lifecycle of admin AI agent runs with heartbeats, trace steps, reports, and outbox events.

## For AI Agents
### Working In This Directory
- Adding an entity: create `.cs` here → add `DbSet` + mapping in `AppDbContext` → `dotnet ef migrations add` → update service/repo as needed.
- Keep classes pure: no methods beyond simple computed properties, no EF attributes, no external package refs.
- Inherit `BaseEntity` for standard entities (gives `Guid Id`, audit timestamps, soft-delete).
- Use `null!` for required navigation properties that EF will populate; initialize collection navs as `new List<T>()`.

### Common Patterns
- IDs are `Guid` (post `InitGuid` migration). Exception: `Role.Id` is `short`.
- JSON columns are `string` properties with names ending in `Jsonb` or `Json`; mapped as `jsonb` in `AppDbContext`.
- Soft-delete is handled via `IsDeleted`/`DeletedAt` on `BaseEntity`; global query filters applied in `AppDbContext`.

## Dependencies
### Internal
- `Common/BaseEntity.cs`, `Common/ISoftDeletable.cs` (base types)
- `Common/` enums used as property types
### External
- None

<!-- MANUAL: -->
