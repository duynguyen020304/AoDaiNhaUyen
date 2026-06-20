<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Interfaces/Repositories

## Purpose
Data-access contracts for querying and persisting domain entities. Repository interfaces are deliberately thin — they expose only the queries and commands the application needs, with no EF Core or SQL concerns leaking into the interface signatures. Implementations live in `AoDaiNhaUyen.Infrastructure/Repositories/`.

## Key Files
| File | Description |
|------|-------------|
| `ICategoryRepository.cs` | `GetActiveAsync()` — returns all active categories ordered by SortOrder |
| `IProductRepository.cs` | `GetPagedAsync(categorySlug, productType, featured, size, page, pageSize)` returns `(IReadOnlyList<Product>, int TotalCount)`; `GetBySlugAsync(slug)` with variants/images/category; `GetBySlugsAsync(slugs)` batch lookup |
| `ICartRepository.cs` | Get user cart, add/update/remove items, clear cart |
| `IUserProfileRepository.cs` | Get user with addresses/orders/order items; profile CRUD; address CRUD |
| `IBlogCategoryRepository.cs` | Blog category CRUD: list, get by slug, create, update, delete |
| `IBlogPostRepository.cs` | Blog post queries: `GetAllAsync` with filters (status, tag, category, search, pagination); `GetBySlugAsync`; create/update/delete |
| `ICommentRepository.cs` | Comment and review queries: get by product, create, get review summaries (batch) |

## For AI Agents

### Working In This Directory
- Repositories are **data-access only** — no business logic, no cross-entity orchestration
- Return Domain entities (`Product`, `Category`, etc.), not DTOs — mapping happens in the service layer
- All methods are async with `CancellationToken cancellationToken = default`
- Paged queries return `(IReadOnlyList<T> Items, int TotalCount)` value tuples
- Add a method here only when a new query pattern is needed; avoid leaking EF-specific parameters (e.g., `IQueryable`, `Expression<>`) into the interface

### Common Patterns
```csharp
// Paged query pattern:
Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
    string? categorySlug, string? productType, bool? featured,
    string? size, int page, int pageSize,
    CancellationToken cancellationToken = default);

// Single entity lookup:
Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
```

## Dependencies
### Internal
- Consumed by: `AoDaiNhaUyen.Application.Services` (CatalogService, BlogPostService, CommentService)
- Implemented by: `AoDaiNhaUyen.Infrastructure/Repositories/`
- Return types are Domain entities from `AoDaiNhaUyen.Domain`

<!-- MANUAL: -->
