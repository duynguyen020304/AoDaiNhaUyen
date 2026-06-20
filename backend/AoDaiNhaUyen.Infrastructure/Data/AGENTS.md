<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Data

## Purpose
Database layer: EF Core `AppDbContext` with all entity mappings, a design-time factory for `dotnet ef` CLI, a startup seed service, and the migrations subfolder. Configures all PostgreSQL-specific features: snake_case naming, native enums, `inet` type, and `jsonb` columns.

## Key Files
| File | Description |
|------|-------------|
| `AppDbContext.cs` | EF Core DbContext — all DbSets, full `OnModelCreating` config (snake_case, constraints, indexes, jsonb, enums, soft-delete filters) |
| `AppDbContextFactory.cs` | `IDesignTimeDbContextFactory<AppDbContext>` — resolves connection string from Api project's appsettings or env vars for `dotnet ef` CLI |
| `SeedDataService.cs` | `ISeedDataService` implementation — runs `MigrateAsync()` then upserts roles, customers, categories, products, style data, AI assets; removes stale records |

## AppDbContext Configuration Details

### Table Naming
- All tables use snake_case: `users`, `user_accounts`, `product_variants`, `chat_messages`, etc.
- All columns auto-convert to snake_case via `ApplySnakeCaseColumnNames()` helper.

### PostgreSQL-Specific Features
- **Enums**: `order_status` (pending/confirmed/processing/shipping/completed/cancelled/returned), `shipping_status` (pending/packed/shipped/delivered/failed/returned)
- **inet type**: `UserSession.IpAddress` stored as PostgreSQL `inet`
- **jsonb columns**: `ChatMessage.UsageJsonb`, `ToolCallsJsonb`, `StructuredPayloadJsonb`; `ChatAttachment.MetadataJsonb`; `ChatThreadMemory.FactsJsonb`, `ResolvedRefsJsonb`; `ProductStyleProfile.StyleKeywordsJsonb`; `OrderItem.CustomMeasurementsJson`

### Key Constraints
- Product types: `ao_dai` or `phu_kien` only
- Order/variant/price amounts: `>= 0`
- Cart item quantity: `> 0`
- Review rating: `1–5`
- User must have email OR phone
- User status: `active`, `inactive`, or `blocked`

### Key Indexes
- Unique: `User.Email`, `User.Phone`, `Category.Slug`, `Product.Slug`, `ProductVariant.Sku`, `Cart.UserId`, `Order.OrderCode`, `Payment.OrderId`
- Lookup: products by category/status/type, chat messages/attachments by thread, orders by user/status/date

## SeedDataService Details
- Invoked when `RunMigrationsAndSeedOnStartup` config flag is true
- **Upsert pattern**: updates existing records when slug/email/SKU matches — safe to run multiple times
- **Seed order**: Roles → Customers → Categories → Products → StyleScenarios → ProductStyleData → ProductAiAssets → RemoveStaleCategories
- Removes stale products by brand "Nha Uyen" and slug; removes empty categories no longer in seed data
- AI assets: resolves curated images from `upload/tryon-curated/{garments,accessories}/` in the Api project

## Migrations Subdirectory
25 migration files. See `Migrations/AGENTS.md` for the full list.

## For AI Agents
### Working In This Directory
- Run migrations from Infrastructure project directory: `dotnet ef migrations add <Name> --startup-project ../AoDaiNhaUyen.Api`
- `AppDbContextFactory` resolves connection string from Api project's appsettings or env vars — no manual connection string needed
- All entity config is inline in `OnModelCreating` — do NOT create separate `IEntityTypeConfiguration<T>` files
- When adding a new entity: add `DbSet<T>` property → add config block in `OnModelCreating` → create migration
- Seed data classes live in `Domain/SeedData/`; `SeedDataService` handles the upsert logic
- Curated try-on assets must exist as physical files in Api's `upload/tryon-curated/` directory

### Common Patterns
- Snake_case applied globally; only override when the auto-conversion is wrong.
- Soft-delete global query filters: `HasQueryFilter(e => !e.IsDeleted)` on all `ISoftDeletable` entities.
- jsonb columns: `entity.Property(e => e.FooJsonb).HasColumnType("jsonb")`.
- Check constraints added via `entity.ToTable(t => t.HasCheckConstraint(...))`.

## Dependencies
### Internal
- `AoDaiNhaUyen.Domain` (entities, seed data)
- `AoDaiNhaUyen.Application` (ISeedDataService interface)
### External
- EF Core, Npgsql EF provider, `Microsoft.EntityFrameworkCore.Design`

<!-- MANUAL: -->
