<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Interfaces

## Purpose
Service and repository contracts defining the boundary between Application and Infrastructure layers. All interfaces here are implemented in `AoDaiNhaUyen.Infrastructure`. The Application layer depends only on these abstractions — never on Infrastructure types directly.

## Root Interfaces
| File | Description |
|------|-------------|
| `ICacheInvalidationService.cs` | Tag-based cache invalidation: `InvalidateDashboardCacheAsync`, `InvalidateOrderRelatedCacheAsync`, `InvalidateProductRelatedCacheAsync`, `InvalidateCategoryRelatedCacheAsync`, `InvalidateInventoryRelatedCacheAsync`, `InvalidateUserRelatedCacheAsync`, `InvalidateAllAsync` |
| `ICacheKeyService.cs` | Cache key generation helpers: builds consistent cache key strings for each domain |
| `IFusionCacheService.cs` | FusionCache abstraction: `GetOrSetAsync`, `GetAsync`, `SetAsync`, `RemoveAsync`, `RemoveByTagAsync`, `InvalidateByPatternAsync` |
| `ISeedDataService.cs` | Database seeding contract: `SeedAllAsync()` |

## Subdirectories
| Directory | Contents |
|-----------|----------|
| `Repositories/` | Data-access contracts: Category, Product, Cart, UserProfile, BlogCategory, BlogPost, Comment |
| `Services/` | ~60 service contracts covering auth, catalog, cart, checkout, blog, chat/AI, admin, marketing, email, storage |

## For AI Agents

### Working In This Directory
- All interfaces use the `I` prefix convention
- Async methods return `Task<T>` with `CancellationToken ct = default` as the last parameter
- `IFusionCacheService` is the primary cache interface — use it with tag arrays so `ICacheInvalidationService` can evict by domain
- Implementations live in `AoDaiNhaUyen.Infrastructure/Services/` and `AoDaiNhaUyen.Infrastructure/Repositories/`
- New feature workflow: define interface here → implement in Infrastructure → register in `ServiceRegistration.cs`

### Common Patterns
```csharp
// Cache-aside in a service:
return await cache.GetOrSetAsync(
    key,
    async ct => await repo.GetDataAsync(ct),
    tags: [CacheTags.Products],
    duration: TimeSpan.FromMinutes(10),
    token: cancellationToken);
```

## Dependencies
### Internal
- Consumed by: `AoDaiNhaUyen.Application.Services` (for cache/repo interfaces)
- Consumed by: `AoDaiNhaUyen.Api` controllers (injected via DI)
- Implemented by: `AoDaiNhaUyen.Infrastructure`

<!-- MANUAL: -->
