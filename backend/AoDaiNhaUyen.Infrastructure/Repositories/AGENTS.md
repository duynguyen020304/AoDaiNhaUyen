<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# Repositories

## Purpose
Repository implementations for data access. Each wraps EF Core queries against AppDbContext and implements matching interface from `AoDaiNhaUyen.Application/Interfaces/Repositories/`.

## Files
| File | Interface | Description |
|------|-----------|-------------|
| `CategoryRepository.cs` | `ICategoryRepository` | Fetches active categories from database. Returns Category entities with navigation properties |
| `ProductRepository.cs` | `IProductRepository` | Paged product listing with optional filters (categorySlug, productType, featured, size). Product detail lookup by slug with eager loading of variants, images, category |
| `CartRepository.cs` | `ICartRepository` | Cart data access: gets cart by user ID with items and variant details. Handles add/update/remove operations and cart clearing |
| `UserProfileRepository.cs` | `IUserProfileRepository` | User data access: gets user with addresses, orders, order items. Supports profile updates, address CRUD, order history queries |

## For AI Agents
### Working In This Directory
- All repositories registered as scoped services in `AoDaiNhaUyen.Api/Configuration/ServiceRegistration.cs`
- Repositories work directly with Domain entities -- no internal mapping to DTOs (happens in services)
- Use EF Core Include/ThenInclude for eager loading of navigation properties
- When adding new repository: create interface in `Application/Interfaces/Repositories/`, implement here, register in ServiceRegistration.cs
- Repositories should only contain data access logic -- no business rules