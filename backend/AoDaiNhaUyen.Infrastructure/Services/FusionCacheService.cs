using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class FusionCacheService : IFusionCacheService
{
  private readonly IFusionCache _cache;
  private readonly ILogger<FusionCacheService> _logger;
  private readonly FusionCacheSettings _settings;
  private readonly IConnectionMultiplexer? _redis;
  private readonly string _distributedCacheInstanceName;

  public FusionCacheService(
    IFusionCache cache,
    ILogger<FusionCacheService> logger,
    IOptions<FusionCacheSettings> settings,
    IConnectionMultiplexer? redis = null,
    string? distributedCacheInstanceName = null)
  {
    _cache = cache;
    _logger = logger;
    _settings = settings.Value;
    _redis = redis;
    _distributedCacheInstanceName = distributedCacheInstanceName ?? string.Empty;
  }

  public async Task<T?> GetOrSetAsync<T>(
    string key,
    Func<CancellationToken, Task<T>> factory,
    string[]? tags = null,
    TimeSpan? duration = null,
    CancellationToken token = default)
  {
    _logger.LogDebug("Cache GetOrSet requested. Key={Key}, Tags={Tags}", key, tags ?? []);

    var options = new FusionCacheEntryOptions
    {
      Duration = duration ?? _settings.L2CacheDuration,
      Size = 1
    };

    if (_settings.EnableFailSafe)
    {
      options.IsFailSafeEnabled = true;
      options.FailSafeMaxDuration = _settings.FailSafeMaxDuration;
    }

    if (_settings.EnableEagerRefresh)
    {
      options.EagerRefreshThreshold = (float)_settings.EagerRefreshThreshold;
    }

    var result = await _cache.GetOrSetAsync<T>(
      key,
      async (ctx, ct) =>
      {
        if (tags is { Length: > 0 })
        {
          ctx.Tags = tags;
        }

        return await factory(ct);
      },
      options,
      token);

    _logger.LogDebug("Cache GetOrSet completed. Key={Key}, HasValue={HasValue}", key, result is not null);
    return result;
  }

  public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
    => await _cache.GetOrDefaultAsync<T>(key, token: token);

  public async Task SetAsync<T>(
    string key,
    T value,
    string[]? tags = null,
    TimeSpan? duration = null,
    CancellationToken token = default)
  {
    var options = new FusionCacheEntryOptions
    {
      Duration = duration ?? _settings.L2CacheDuration,
      Size = 1
    };

    await _cache.SetAsync(key, value, options, tags, token);
  }

  public async Task RemoveAsync(string key, CancellationToken token = default)
    => await _cache.RemoveAsync(key, token: token);

  public async Task RemoveByTagAsync(string tag, CancellationToken token = default)
  {
    try
    {
      await _cache.RemoveByTagAsync(tag, token: token);
      _logger.LogInformation("Invalidated cache tag. Tag={Tag}", tag);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to invalidate cache tag. Tag={Tag}", tag);
    }
  }

  public async Task<int> InvalidateByPatternAsync(string pattern, CancellationToken token = default)
  {
    if (_redis is null)
    {
      _logger.LogWarning("Redis unavailable for pattern invalidation. Pattern={Pattern}", pattern);
      return 0;
    }

    var db = _redis.GetDatabase();
    var count = 0;
    var redisPattern = BuildRedisPattern(pattern);

    foreach (var endpoint in _redis.GetEndPoints())
    {
      var server = _redis.GetServer(endpoint);
      foreach (var key in server.Keys(pattern: redisPattern, pageSize: 100).Take(1000))
      {
        var redisKey = key.ToString();
        if (string.IsNullOrWhiteSpace(redisKey)) continue;

        try
        {
          await _cache.RemoveAsync(StripDistributedCachePrefix(redisKey), token: token);
          count++;
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "FusionCache remove failed; deleting raw Redis key. Key={Key}", redisKey);
          if (await db.KeyDeleteAsync(redisKey)) count++;
        }
      }
    }

    _logger.LogInformation("Invalidated {Count} cache keys by pattern. Pattern={Pattern}", count, pattern);
    return count;
  }

  private string BuildRedisPattern(string pattern)
    => string.IsNullOrWhiteSpace(_distributedCacheInstanceName)
      ? pattern
      : $"{_distributedCacheInstanceName}{pattern}";

  private string StripDistributedCachePrefix(string redisKey)
    => !string.IsNullOrWhiteSpace(_distributedCacheInstanceName) && redisKey.StartsWith(_distributedCacheInstanceName, StringComparison.Ordinal)
      ? redisKey[_distributedCacheInstanceName.Length..]
      : redisKey;
}
