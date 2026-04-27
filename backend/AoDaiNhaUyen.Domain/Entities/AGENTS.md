<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Entities

## Purpose
Domain entity classes mirror database schema. Plain C# classes with properties + EF Core navigation properties. No shared base class; BaseEntity exists but main entities unused it.

## Files
| File | Key Fields | Description |
|------|-----------|-------------|
| `BaseEntity.cs` | Id (int) | Abstract base entity -- currently unused; main entities use `long Id` directly |
| `Cart.cs` | Id, UserId, Items | Shopping cart -- one per user (unique index). Has CartItems collection |
| `CartItem.cs` | Id, CartId, VariantId, Quantity | Cart line item. Unique on (CartId, VariantId). Quantity must be > 0 |
| `Category.cs` | Id, Parent?, Name, Slug, SortOrder, IsActive | Hierarchical category with self-referencing Parent. Slug unique. Tree via Children collection |
| `ChatAttachment.cs` | Id, ThreadId, MessageId?, Kind, FileUrl, MimeType, FileSizeBytes, MetadataJsonb | File attached to chat thread/message. Kind usually "user_image" |
| `ChatMessage.cs` | Id, ThreadId, Role, Content, Intent, ClientMessageId, PromptVersion, UsageJsonb, FinishReason, ToolCallsJsonb, StructuredPayloadJsonb | Stylist chat thread message. Role "user" or "assistant". AI metadata in jsonb columns |
| `ChatThread.cs` | Id, UserId?, GuestKeyHash?, Status, Source, ClaimedAt | Chat conversation thread. Supports authenticated users + anonymous guests |
| `ChatThreadMemory.cs` | ThreadId (PK), Summary, FactsJsonb, ResolvedRefsJsonb, LastMessageId? | Per-thread memory state. One-to-one with ChatThread. Stores extracted facts + resolved product refs |
| `EmailVerificationToken.cs` | Id, UserId, Token, ExpiresAt, UsedAt? | Email verification token with expiration. Token unique |
| `MeasurementProfile.cs` | Id, UserId, ProfileName, HeightCm, WeightKg, BustCm, WaistCm, HipCm, ShoulderCm, SleeveLengthCm, DressLengthCm | Body measurements for custom tailoring. All measurements nullable decimal (numeric(5,2)) |
| `Order.cs` | Id, OrderCode, UserId, AddressId?, recipient info, Province/District/Ward/AddressLine, Subtotal, DiscountAmount, ShippingFee, TotalAmount, OrderStatus, lifecycle timestamps | Customer order with full shipping address snapshot. Status lifecycle: pending -> confirmed -> processing -> shipping -> completed/cancelled/returned |
| `OrderItem.cs` | Id, OrderId, ProductId?, VariantId?, ProductName, Sku, Size, Color, UnitPrice, Quantity, LineTotal, IsCustomTailoring, MeasurementProfileId?, CustomMeasurementsJson | Order line item. Supports custom tailoring with measurement profile ref |
| `PasswordResetToken.cs` | Id, UserId, Token, ExpiresAt, UsedAt? | Password reset token with expiration. Token unique |
| `Payment.cs` | Id, OrderId, Amount, PaidAt | Payment record. One-to-one with Order |
| `Product.cs` | Id, CategoryId, Name, Slug, ProductType, ShortDescription, Description, Material, Brand, Origin, Status, IsFeatured | Main product entity. ProductType "ao_dai" or "phu_kien". Collections: Variants, Images, StyleProfiles, Scenarios, Pairings, AiAssets |
| `ProductAiAsset.cs` | Id, ProductId, VariantId?, AssetKind, FileUrl, MimeType, IsActive | AI asset for try-on. AssetKind: tryon_garment, tryon_accessory, tryon_garment_curated, tryon_accessory_curated |
| `ProductImage.cs` | Id, ProductId, VariantId?, ImageUrl, AltText, SortOrder, IsPrimary | Product image. IsPrimary flag for main image. Sortable by SortOrder |
| `ProductPairing.cs` | Id, BaseProductId, PairedProductId, ScenarioId?, Score, Notes | Product pairing recommendation with compatibility score. Unique on (BaseProductId, PairedProductId, ScenarioId) |
| `ProductScenario.cs` | ProductId, ScenarioId, Score, Notes | Product-to-scenario mapping with score. Composite PK (ProductId, ScenarioId) |
| `ProductStyleProfile.cs` | Id, ProductId, StyleKeywordsJsonb, Formality, Silhouette, PrimaryColorFamily, SecondaryColorFamily, Notes | Style metadata for product. One-to-one with Product (unique index on ProductId) |
| `ProductVariant.cs` | Id, ProductId, Sku, VariantName, Size, Color, Price, SalePrice?, StockQty, IsDefault, Status | Product variant with SKU. Unique on (ProductId, Size, Color). Price fields numeric(12,2) |
| `Review.cs` | Id, UserId, ProductId, OrderItemId?, Rating, Comment, IsVisible | Product review. Rating 1-5. Unique on (UserId, ProductId, OrderItemId) |
| `Role.cs` | Id (short), Name | User role. Id uses short type with identity column |
| `Shipment.cs` | Id, OrderId, Carrier?, TrackingNumber?, ShippingStatus, ShippedAt?, DeliveredAt? | Shipping record. Status: pending/packed/shipped/delivered/failed/returned |
| `StyleScenario.cs` | Id, Slug, Name, Description, IsActive | Style scenario definition (giao-vien, le-tet, du-tiec, chup-anh) |
| `User.cs` | Id, FullName, Email?, Phone?, Gender?, DateOfBirth?, AvatarUrl?, Status, EmailVerifiedAt?, PhoneVerifiedAt?, LastLoginAt? | Core user entity. Email and Phone have unique indexes. Must have email OR phone (check constraint). Collections: UserAccounts, UserRoles, Sessions, Tokens, Addresses, MeasurementProfiles, Orders, Carts, Reviews |
| `UserAccount.cs` | Id, UserId, Provider, ProviderAccountId, PasswordHash?, IsVerified | OAuth/credential account. Unique on (Provider, ProviderAccountId). Provider: credentials, google, facebook |
| `UserAddress.cs` | Id, UserId, RecipientName, RecipientPhone, Province, District, Ward?, AddressLine, IsDefault | Shipping address. Vietnamese address structure: Province/District/Ward |
| `UserRole.cs` | UserId, RoleId | Many-to-many join. Composite PK (UserId, RoleId) |
| `UserSession.cs` | Id, UserId, RefreshTokenHash, UserAgent?, IpAddress?, ExpiresAt, RevokedAt? | Refresh token session. IpAddress stored as PostgreSQL inet type |

## For AI Agents
### Working In This Directory
- Pure POCO classes -- no attributes, no EF Core refs in this project
- EF Core config (table names, constraints, indexes) done in `AppDbContext.OnModelCreating`
- Most entities use `long Id`; Role uses `short Id`; some use composite keys
- Navigation properties initialized as empty collections (`= new List<T>()`)
- Foreign key navigation properties initialized as `null!` (not-null assertion)
- Vietnamese business context: ao dai = traditional dress, phu kien = accessories, giao vien = teacher, le tet = holiday, du tiec = event/party, chup anh = photo shoot
- Adding new entity: create class here, add DbSet in AppDbContext, configure relationships/constraints in OnModelCreating, add migration