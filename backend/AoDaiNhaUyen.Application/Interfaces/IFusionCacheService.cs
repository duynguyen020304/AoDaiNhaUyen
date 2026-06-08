namespace AoDaiNhaUyen.Application.Interfaces;

public interface IFusionCacheService
{
  Task<T?> GetOrSetAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    string[]? tags = null,
    TimeSpan? duration = null,
    CancellationToken token = default);

  Task<T?> GetAsync<T>(string key, CancellationToken token = default);

  Task SetAsync<T>(
    string key,
    T value,
    string[]? tags = null,
    TimeSpan? duration = null,
    CancellationToken token = default);

  Task RemoveAsync(string key, CancellationToken token = default);
  Task RemoveByTagAsync(string tag, CancellationToken token = default);
  Task<int> InvalidateByPatternAsync(string pattern, CancellationToken token = default);
}
