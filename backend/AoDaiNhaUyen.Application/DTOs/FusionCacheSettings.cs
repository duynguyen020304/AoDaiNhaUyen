namespace AoDaiNhaUyen.Application.DTOs;

public sealed class FusionCacheSettings
{
  public bool EnableL2Cache { get; set; } = true;
  public bool EnableBackplane { get; set; } = true;
  public int L1CacheSize { get; set; } = 5000;
  public TimeSpan L1CacheDuration { get; set; } = TimeSpan.FromMinutes(1);
  public TimeSpan L2CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
  public bool EnableFailSafe { get; set; } = true;
  public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromMinutes(5);
  public bool EnableEagerRefresh { get; set; }
  public double EagerRefreshThreshold { get; set; } = 0.9;
  public Dictionary<string, int> CacheDurations { get; set; } = [];
}
