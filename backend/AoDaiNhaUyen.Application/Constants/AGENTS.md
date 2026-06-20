<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Constants

## Purpose
Shared string constants used across the Application and Infrastructure layers. Currently contains cache tag identifiers used by `ICacheInvalidationService` and `IFusionCacheService` to group and invalidate related cache entries.

## Key Files
| File | Description |
|------|-------------|
| `CacheTags.cs` | Static `CacheTags` class — `Dashboard`, `Orders`, `Products`, `Categories`, `Inventory`, `Users`, `Blog` tag strings in `"tag:<name>"` format |

## For AI Agents

### Working In This Directory
- All constants are `public const string` on `public static` classes
- Tag strings use the `"tag:<name>"` prefix convention understood by FusionCache tag-based eviction
- Add a new tag here when introducing a new cache domain; then wire it into `CacheInvalidationService` and the relevant service's `GetOrSetAsync` calls

### Common Patterns
```csharp
// Usage in a service:
await cache.GetOrSetAsync(key, factory, tags: [CacheTags.Products], duration: TimeSpan.FromMinutes(10), token);

// Invalidation:
await cache.RemoveByTagAsync(CacheTags.Products, cancellationToken);
```

## Dependencies
### Internal
- Referenced by `Services/` (BlogPostService, CacheInvalidationService, CatalogService, etc.)
- Referenced by `Interfaces/ICacheInvalidationService.cs`

<!-- MANUAL: -->
