<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Services

## Purpose
Application-layer service implementations — the subset of business logic that lives in Application rather than Infrastructure because it coordinates repositories and other services without requiring external I/O (database, HTTP, storage). Currently contains catalog, blog, comment, and cache management services. Most service interfaces defined in `Interfaces/Services/` are implemented in `AoDaiNhaUyen.Infrastructure/Services/`.

## Key Files
| File | Description |
|------|-------------|
| `CatalogService.cs` | Implements `ICatalogService`: category flat/tree queries, paged product listing with image URL resolution, product detail with variants, images, and review summary. Coordinates `ICategoryRepository`, `IProductRepository`, `IImageVisibilityService`, and `ICommentService`. |
| `BlogPostService.cs` | Implements `IBlogPostService`: paged post list with filters (status, tag, category, search), post detail by slug, create/update/delete with cache invalidation. Uses `IFusionCacheService` with `CacheTags.Blog` for cache-aside. |
| `BlogCategoryService.cs` | Implements `IBlogCategoryService`: blog category CRUD with cache invalidation on writes. |
| `CommentService.cs` | Implements `ICommentService`: get comments/reviews by product, create comment/review, get review summary, batch review summaries for product lists. |
| `CacheInvalidationService.cs` | Implements `ICacheInvalidationService`: tag-based cache eviction groups — Dashboard, Orders, Products, Categories, Inventory, Users, Blog. Calls `IFusionCacheService.RemoveByTagAsync()` for each relevant tag. |
| `CacheKeyService.cs` | Implements `ICacheKeyService`: builds deterministic cache key strings by domain and parameters. |

## For AI Agents

### Working In This Directory
- These are **Application-layer** services — they may reference `Interfaces/` and `Domain` but not Infrastructure types directly
- Use constructor injection (primary constructor syntax preferred): `public sealed class FooService(IFooRepo repo, ICache cache) : IFooService`
- Cache-aside pattern: call `IFusionCacheService.GetOrSetAsync(key, factory, tags, duration, token)`; pass relevant `CacheTags.*` so `CacheInvalidationService` can evict
- After write operations (create/update/delete), call the appropriate `ICacheInvalidationService.Invalidate*Async()` method
- `IHermesEventOutboxPublisher` is injected into blog/order services to fire domain events after mutations

### Common Patterns
```csharp
// Cache-aside in a service method:
return await cache.GetOrSetAsync(
    $"catalog:product:{slug}",
    async ct => await productRepo.GetBySlugAsync(slug, ct),
    tags: [CacheTags.Products],
    duration: TimeSpan.FromMinutes(30),
    token: cancellationToken);

// Post-write cache invalidation:
await cacheInvalidation.InvalidateProductRelatedCacheAsync(cancellationToken);

// Batch review summary (avoid N+1):
var summaries = await commentService.GetReviewSummariesAsync(productIds, ct);
```

## Dependencies
### Internal
- `Interfaces/Repositories/` — `IProductRepository`, `ICategoryRepository`, `IBlogPostRepository`, `IBlogCategoryRepository`, `ICommentRepository`
- `Interfaces/` root — `IFusionCacheService`, `ICacheInvalidationService`
- `Interfaces/Services/` — `IImageVisibilityService`, `ICommentService`, `IHermesEventOutboxPublisher`
- `Constants/CacheTags.cs` — cache tag strings
- `AoDaiNhaUyen.Domain` — entity types returned by repositories

### External
- `Microsoft.Extensions.Logging` — `ILogger<T>` for debug/info logging in `CacheInvalidationService`

<!-- MANUAL: -->
