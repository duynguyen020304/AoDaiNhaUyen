<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Repositories

## Purpose
EF Core repository implementations. Each class wraps `AppDbContext` queries and returns Domain entities or paged domain results. DTO mapping happens in services, not here. Implements interfaces defined in `Application/Interfaces/Repositories/`.

## Key Files
| File | Interface | Notes |
|------|-----------|-------|
| `CategoryRepository.cs` | `ICategoryRepository` | Active category queries and category tree inputs |
| `ProductRepository.cs` | `IProductRepository` | Product list/detail with filters, pagination, and eager loading of variants/images |
| `CartRepository.cs` | `ICartRepository` | Cart and cart item reads and mutations |
| `UserProfileRepository.cs` | `IUserProfileRepository` | User profile, saved addresses, order history, order items |
| `BlogCategoryRepository.cs` | `IBlogCategoryRepository` | Blog category lookups |
| `BlogPostRepository.cs` | `IBlogPostRepository` | Blog listing/detail/admin CRUD with status filters |
| `CommentRepository.cs` | `ICommentRepository` | Product comment persistence and paged queries |

## For AI Agents
### Working In This Directory
- Keep business rules out of repositories — services decide behavior; repos only query/persist.
- Use `Include`/`ThenInclude` only where the caller needs the related graph data.
- Prefer `AsNoTracking()` for read-only queries; use tracked queries only when a mutation follows.
- Register new repos as scoped services in `Api/Configuration/ServiceRegistration.cs`.
- For paged reads, apply a deterministic `OrderBy` before `Skip`/`Take`.
- Match the existing primary constructor DI pattern: `public class FooRepository(AppDbContext db)`.

### Common Patterns
- Primary constructor DI: `public class ProductRepository(AppDbContext db) : IProductRepository`.
- Soft-deleted records excluded automatically via global query filter on `AppDbContext`; no manual `!IsDeleted` needed.
- PostgreSQL-specific query behavior (e.g., case-insensitive ILIKE) should be visible in comments or documented in the calling service.
- Paged results return `(IReadOnlyList<T> items, int totalCount)` tuple or a `PagedResult<T>` type.

## Dependencies
### Internal
- `Data/AppDbContext.cs` (injected)
- `AoDaiNhaUyen.Domain` (entity types returned)
- `AoDaiNhaUyen.Application` (repository interfaces implemented)
### External
- EF Core, Npgsql

<!-- MANUAL: -->
