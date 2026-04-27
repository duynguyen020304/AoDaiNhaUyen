<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen.Domain

## Purpose
Domain layer holds all entity definitions + seed data. Standalone app core -- no dependencies on other projects. Entities = plain C# classes with navigation properties for EF Core relationships.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Domain.csproj` | Project file -- no project references (standalone) |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Entities/` | All domain entity classes (see `Entities/AGENTS.md`) |
| `SeedData/` | Static seed data classes for initial database population |

## Entities Overview

### User & Auth Entities
| File | Description |
|------|-------------|
| `User.cs` | Core user entity with FullName, Email, Phone, Gender, DateOfBirth, Status; collections for accounts, roles, sessions, tokens, addresses, orders, carts, reviews |
| `UserAccount.cs` | OAuth/credential accounts linked to user: Provider (credentials/google/facebook), ProviderAccountId, PasswordHash, IsVerified |
| `UserRole.cs` | Many-to-many join between User and Role |
| `Role.cs` | User roles (e.g., "customer", "admin") |
| `UserSession.cs` | Refresh token sessions with token hash, UserAgent, IpAddress (inet type), ExpiresAt, RevokedAt |
| `EmailVerificationToken.cs` | Email verification tokens with expiration + usage tracking |
| `PasswordResetToken.cs` | Password reset tokens with expiration + usage tracking |
| `UserAddress.cs` | Shipping addresses: RecipientName, Phone, Province, District, Ward, AddressLine, IsDefault |

### Product & Catalog Entities
| File | Description |
|------|-------------|
| `Category.cs` | Hierarchical categories with self-referencing Parent, Slug, SortOrder, IsActive; supports tree structure |
| `Product.cs` | Main product entity: Name, Slug, ProductType (ao_dai/phu_kien), Material, Brand, Status, IsFeatured; collections for variants, images, style profiles, scenarios, pairings, AI assets |
| `ProductVariant.cs` | Product variants: SKU, Size, Color, Price, SalePrice, StockQty, IsDefault, Status |
| `ProductImage.cs` | Product images: ImageUrl, AltText, SortOrder, IsPrimary; optionally linked to variant |

### AI & Styling Entities
| File | Description |
|------|-------------|
| `ProductAiAsset.cs` | AI assets per product: AssetKind (tryon_garment/tryon_accessory/curated variants), FileUrl, MimeType, IsActive |
| `ProductStyleProfile.cs` | Style metadata: StyleKeywordsJsonb, Formality, Silhouette, color families; one-to-one with Product |
| `ProductScenario.cs` | Product-to-scenario mapping with compatibility score |
| `ProductPairing.cs` | Product pairing recommendations with score, linked to scenario |
| `StyleScenario.cs` | Style scenarios (giao-vien, le-tet, du-tiec, chup-anh) |

### Chat Entities
| File | Description |
|------|-------------|
| `ChatThread.cs` | Chat conversation thread: UserId or GuestKeyHash for anonymous, Status, Source, ClaimedAt |
| `ChatMessage.cs` | Messages in thread: Role (user/assistant), Content, Intent, PromptVersion, UsageJsonb, ToolCallsJsonb, StructuredPayloadJsonb |
| `ChatAttachment.cs` | File attachments in chat: Kind (user_image), FileUrl, MimeType, MetadataJsonb |
| `ChatThreadMemory.cs` | Per-thread memory state: Summary, FactsJsonb, ResolvedRefsJsonb, LastMessageId |

### Shopping & Order Entities
| File | Description |
|------|-------------|
| `Cart.cs` | Shopping cart: one per user (unique index on UserId) |
| `CartItem.cs` | Cart items: VariantId, Quantity; unique constraint on (CartId, VariantId) |
| `Order.cs` | Orders: OrderCode, recipient info, Province/District/Ward/AddressLine, amounts (Subtotal, Discount, Shipping, Total), OrderStatus lifecycle timestamps |
| `OrderItem.cs` | Order line items: ProductName, SKU, Size, Color, UnitPrice, Quantity, LineTotal, custom tailoring support |
| `Payment.cs` | Payment records: Amount, PaidAt; one-to-one with Order |
| `Shipment.cs` | Shipping tracking: Carrier, TrackingNumber, ShippingStatus |
| `Review.cs` | Product reviews: Rating (1-5), Comment, IsVisible |

### Other Entities
| File | Description |
|------|-------------|
| `BaseEntity.cs` | Abstract base entity with int Id (currently unused by main entities which use long Id) |
| `MeasurementProfile.cs` | User body measurements: HeightCm, WeightKg, BustCm, WaistCm, HipCm, ShoulderCm, SleeveLengthCm, DressLengthCm |

## SeedData
| File | Description |
|------|-------------|
| `DefaultRoles.cs` | Default role names (admin, customer, etc.) |
| `DefaultCustomers.cs` | Seed customer accounts with hashed passwords |
| `DefaultCategories.cs` | Category tree with parent-child relationships |
| `DefaultProducts.cs` | Product catalog with variants + images |
| `DefaultMaterials.cs` | Material name mappings by slug |
| `DefaultStoreLocations.cs` | Default store location data |

## For AI Agents
### Working In This Directory
- Project has **zero external dependencies** -- only plain C# classes
- Entities do NOT use BaseEntity for main tables -- most use `long Id` directly (Cart, Category, Product, etc.)
- All entity properties use `long` for Id (not `int`), except Role uses `short`
- Navigation properties initialized as empty collections
- JSON columns stored as `string? ...Jsonb` properties with `[Column("jsonb")]` configured in AppDbContext
- ProductType constrained to `ao_dai` or `phu_kien` via database check constraint
- Vietnamese e-commerce context: ao dai (traditional Vietnamese dress), phu kien (accessories)
- When adding new entity, also add DbSet in AppDbContext, configure in OnModelCreating, add migration
- Seed data classes static -- provide data for SeedDataService in Infrastructure