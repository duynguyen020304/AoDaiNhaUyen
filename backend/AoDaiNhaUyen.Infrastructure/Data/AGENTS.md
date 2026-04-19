<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Data

## Purpose
Database layer containing the EF Core DbContext, design-time factory, seed service, and migrations. The DbContext configures all entity mappings with PostgreSQL-specific features (snake_case, enums, inet, jsonb).

## Files
| File | Description |
|------|-------------|
| `AppDbContext.cs` | EF Core DbContext with 24 DbSets and comprehensive entity configuration in `OnModelCreating` |
| `AppDbContextFactory.cs` | `IDesignTimeDbContextFactory<AppDbContext>` for `dotnet ef` CLI commands |
| `SeedDataService.cs` | `ISeedDataService` implementation: runs migrations, seeds roles, customers, categories, products, style data, and AI assets |

## AppDbContext Configuration Details

### Table Naming
- All tables use snake_case: `users`, `user_accounts`, `product_variants`, `chat_messages`, etc.
- All columns auto-converted to snake_case via `ApplySnakeCaseColumnNames()` helper

### PostgreSQL-Specific Features
- **Enums**: `order_status` (pending/confirmed/processing/shipping/completed/cancelled/returned), `shipping_status` (pending/packed/shipped/delivered/failed/returned)
- **inet type**: UserSession.IpAddress stored as PostgreSQL `inet`
- **jsonb columns**: ChatMessage.UsageJsonb, ToolCallsJsonb, StructuredPayloadJsonb; ChatAttachment.MetadataJsonb; ChatThreadMemory.FactsJsonb, ResolvedRefsJsonb; ProductStyleProfile.StyleKeywordsJsonb; OrderItem.CustomMeasurementsJson

### Key Constraints
- Product types: `ao_dai` or `phu_kien` only
- Order/variant/price amounts: `>= 0`
- Cart item quantity: `> 0`
- Review rating: `1-5`
- User must have email OR phone
- User status: `active`, `inactive`, or `blocked`

### Key Indexes
- Unique: User.Email, User.Phone, Category.Slug, Product.Slug, ProductVariant.Sku, Cart.UserId, Order.OrderCode, Payment.OrderId
- Lookup: products by category/status/type, chat messages/attachments by thread, orders by user/status/date

## SeedDataService Details
- Called when `RunMigrationsAndSeedOnStartup` config is true
- **Upsert pattern**: updates existing records if slug/email/SKU matches
- **Seed order**: Roles -> Customers -> Categories -> Products -> StyleScenarios -> ProductStyleData -> ProductAiAssets -> RemoveStaleCategories
- Removes stale products (by brand "Nha Uyen" and slug) and empty categories no longer in seed data
- AI assets: resolves curated images from `upload/tryon-curated/{garments,accessories}/` directory
- Style inference: derives color families, formality levels, style keywords, and scenario scores from product slugs and category

## Migrations/ Subdirectory
See `Migrations/AGENTS.md` for individual migration files.

## For AI Agents
### Working In This Directory
- Run `dotnet ef migrations add <Name> --startup-project ../AoDaiNhaUyen.Api` from Infrastructure project directory
- `AppDbContextFactory` resolves connection string from Api project's appsettings or env vars
- All entity configuration is inline in `OnModelCreating` -- no separate EntityTypeConfiguration classes
- When adding a new entity: add DbSet property, add entity configuration in OnModelCreating, create migration
- Seed data classes are in `AoDaiNhaUyen.Domain/SeedData/`
- The seeder handles idempotent upserts -- safe to run multiple times
- Curated try-on assets must exist as physical files in the Api's `upload/tryon-curated/` directory
