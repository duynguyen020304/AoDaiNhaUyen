namespace AoDaiNhaUyen.Application.Interfaces;

public interface ICacheInvalidationService
{
  Task InvalidateDashboardCacheAsync(CancellationToken cancellationToken = default);
  Task InvalidateOrderRelatedCacheAsync(CancellationToken cancellationToken = default);
  Task InvalidateProductRelatedCacheAsync(CancellationToken cancellationToken = default);
  Task InvalidateUserRelatedCacheAsync(CancellationToken cancellationToken = default);
  Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}
