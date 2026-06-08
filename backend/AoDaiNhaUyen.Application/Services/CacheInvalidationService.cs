using AoDaiNhaUyen.Application.Constants;
using AoDaiNhaUyen.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Application.Services;

public sealed class CacheInvalidationService(
  IFusionCacheService cache,
  ILogger<CacheInvalidationService> logger) : ICacheInvalidationService
{
  public async Task InvalidateDashboardCacheAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Invalidating dashboard cache");
    await cache.RemoveByTagAsync(CacheTags.Dashboard, cancellationToken);
  }

  public async Task InvalidateOrderRelatedCacheAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Invalidating order-related caches");
    await Task.WhenAll(
      cache.RemoveByTagAsync(CacheTags.Dashboard, cancellationToken),
      cache.RemoveByTagAsync(CacheTags.Orders, cancellationToken));
  }

  public async Task InvalidateProductRelatedCacheAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Invalidating product-related caches");
    await Task.WhenAll(
      cache.RemoveByTagAsync(CacheTags.Dashboard, cancellationToken),
      cache.RemoveByTagAsync(CacheTags.Products, cancellationToken));
  }

  public async Task InvalidateUserRelatedCacheAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Invalidating user-related caches");
    await Task.WhenAll(
      cache.RemoveByTagAsync(CacheTags.Dashboard, cancellationToken),
      cache.RemoveByTagAsync(CacheTags.Users, cancellationToken));
  }

  public async Task InvalidateAllAsync(CancellationToken cancellationToken = default)
  {
    logger.LogDebug("Invalidating all app caches");
    await Task.WhenAll(
      cache.RemoveByTagAsync(CacheTags.Dashboard, cancellationToken),
      cache.RemoveByTagAsync(CacheTags.Orders, cancellationToken),
      cache.RemoveByTagAsync(CacheTags.Products, cancellationToken),
      cache.RemoveByTagAsync(CacheTags.Users, cancellationToken));
  }
}
