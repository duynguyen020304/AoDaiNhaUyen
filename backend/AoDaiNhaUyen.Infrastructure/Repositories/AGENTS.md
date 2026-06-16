<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# Repositories

## Purpose
EF Core data access implementations. Repositories wrap `AppDbContext` queries and return Domain entities or paged domain results; DTO mapping happens in services.

## Files
| File | Interface | Notes |
|------|-----------|-------|
| `CategoryRepository.cs` | `ICategoryRepository` | Active category queries/tree inputs |
| `ProductRepository.cs` | `IProductRepository` | Product list/detail with filters and eager loading |
| `CartRepository.cs` | `ICartRepository` | Cart/item reads and mutations |
| `UserProfileRepository.cs` | `IUserProfileRepository` | Profile, addresses, orders, order items |
| `BlogCategoryRepository.cs` | `IBlogCategoryRepository` | Blog category lookups |
| `BlogPostRepository.cs` | `IBlogPostRepository` | Blog listing/detail/admin CRUD inputs |
| `CommentRepository.cs` | `ICommentRepository` | Product comment persistence/queries |

## Local Conventions
- Keep business rules out; services decide behavior.
- Use `Include`/`ThenInclude` only where caller needs graph data.
- Prefer `AsNoTracking()` for read-only queries unless mutation follows.
- Register new repos as scoped services in `ServiceRegistration.cs`.
- For paged reads, return deterministic ordering before `Skip`/`Take`.
- Keep PostgreSQL-specific query assumptions visible in tests or service callers.
