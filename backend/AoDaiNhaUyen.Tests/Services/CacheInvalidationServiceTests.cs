using Xunit;
using AoDaiNhaUyen.Application.Constants;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class CacheInvalidationServiceTests
{
  [Fact]
  public async Task InvalidateProductRelatedCacheAsync_RemovesProductCategoryInventoryDashboardTags()
  {
    var cache = new RecordingFusionCacheService();
    var service = new CacheInvalidationService(cache, NullLogger<CacheInvalidationService>.Instance);

    await service.InvalidateProductRelatedCacheAsync();

    AssertTags(cache, CacheTags.Dashboard, CacheTags.Products, CacheTags.Categories, CacheTags.Inventory);
  }

  [Fact]
  public async Task InvalidateCategoryRelatedCacheAsync_RemovesCategoryProductDashboardTags()
  {
    var cache = new RecordingFusionCacheService();
    var service = new CacheInvalidationService(cache, NullLogger<CacheInvalidationService>.Instance);

    await service.InvalidateCategoryRelatedCacheAsync();

    AssertTags(cache, CacheTags.Dashboard, CacheTags.Categories, CacheTags.Products);
  }

  [Fact]
  public async Task InvalidateOrderRelatedCacheAsync_RemovesOrderInventoryDashboardTags()
  {
    var cache = new RecordingFusionCacheService();
    var service = new CacheInvalidationService(cache, NullLogger<CacheInvalidationService>.Instance);

    await service.InvalidateOrderRelatedCacheAsync();

    AssertTags(cache, CacheTags.Dashboard, CacheTags.Orders, CacheTags.Inventory);
  }

  private static void AssertTags(RecordingFusionCacheService cache, params string[] expectedTags)
  {
    Assert.Equal(expectedTags.OrderBy(tag => tag), cache.RemovedTags.OrderBy(tag => tag));
  }

  private sealed class RecordingFusionCacheService : IFusionCacheService
  {
    public List<string> RemovedTags { get; } = [];

    public Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, string[]? tags = null, TimeSpan? duration = null, CancellationToken token = default) =>
      throw new NotSupportedException();

    public Task<T?> GetAsync<T>(string key, CancellationToken token = default) =>
      throw new NotSupportedException();

    public Task SetAsync<T>(string key, T value, string[]? tags = null, TimeSpan? duration = null, CancellationToken token = default) =>
      throw new NotSupportedException();

    public Task RemoveAsync(string key, CancellationToken token = default) =>
      throw new NotSupportedException();

    public Task RemoveByTagAsync(string tag, CancellationToken token = default)
    {
      RemovedTags.Add(tag);
      return Task.CompletedTask;
    }

    public Task<int> InvalidateByPatternAsync(string pattern, CancellationToken token = default) =>
      throw new NotSupportedException();
  }
}
