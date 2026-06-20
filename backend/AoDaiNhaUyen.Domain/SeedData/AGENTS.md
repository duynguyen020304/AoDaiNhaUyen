<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# SeedData

## Purpose
Static seed data classes used by `Infrastructure/Data/SeedDataService.cs` to populate the database on startup. All data is defined as `IReadOnlyList<T>` on `static` classes — no runtime logic here. The seeder handles upserts so running multiple times is safe.

## Key Files
| File | Description |
|------|-------------|
| `DefaultRoles.cs` | Two roles: `admin`, `customer` (list of strings) |
| `DefaultCustomers.cs` | Seed admin and customer user accounts with hashed passwords |
| `DefaultCategories.cs` | Category tree: `ao-dai` + `phu-kien` roots with child subcategories; uses `SeedCategory` record |
| `DefaultProducts.cs` | Full product catalog: ao dai and accessories with variants, images, and style data |
| `DefaultMaterials.cs` | Fabric/material definitions (name, slug, description, swatch image URL); uses `SeedMaterial` record |
| `DefaultStoreLocations.cs` | Physical store location data |

## For AI Agents
### Working In This Directory
- Add new seed files here when you need new default data at startup.
- Keep records immutable (`sealed record` or `readonly` struct).
- `DefaultCategories` uses `SeedCategory(Name, Slug, SortOrder, ParentSlug?)` — `ParentSlug` links to another category's slug.
- `DefaultMaterials` uses `SeedMaterial(Name, Slug, Description, SwatchImageUrl)`.
- After adding/changing seed data, verify `SeedDataService` in Infrastructure handles upsert logic for the new type.
- Stale categories/products are removed by the seeder — check `SeedDataService` cleanup logic when renaming slugs.

### Common Patterns
- Static classes with a single `public static readonly IReadOnlyList<T> Items` property.
- Slugs must be URL-safe, lowercase, hyphenated Vietnamese romanization.
- Product type values: `ao_dai` or `phu_kien`.
- Seed order enforced by `SeedDataService`: Roles → Customers → Categories → Products → Style data → AI assets → cleanup.

## Dependencies
### Internal
- Consumed exclusively by `Infrastructure/Data/SeedDataService.cs`
### External
- None

<!-- MANUAL: -->
