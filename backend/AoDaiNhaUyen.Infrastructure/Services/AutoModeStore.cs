using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>
/// In-memory per-admin store for AI autonomy mode.
/// Entries expire quickly so medium-risk auto-approval cannot stay enabled forever.
/// High/Critical tools still require human confirmation.
/// </summary>
public sealed class AutoModeStore : IAutoModeStore
{
  private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
  private readonly object _lock = new();
  private readonly Dictionary<Guid, DateTime> _enabledUntilByAdmin = new();

  public bool IsAutoMode
  {
    get
    {
      lock (_lock)
      {
        PruneExpired(DateTime.UtcNow);
        return _enabledUntilByAdmin.Count > 0;
      }
    }
  }

  public bool IsAutoModeEnabled(Guid adminUserId)
  {
    lock (_lock)
    {
      PruneExpired(DateTime.UtcNow);
      return _enabledUntilByAdmin.ContainsKey(adminUserId);
    }
  }

  public void Enable(Guid adminUserId)
  {
    lock (_lock)
    {
      PruneExpired(DateTime.UtcNow);
      _enabledUntilByAdmin[adminUserId] = DateTime.UtcNow.Add(Ttl);
    }
  }

  public void Disable(Guid adminUserId)
  {
    lock (_lock)
    {
      _enabledUntilByAdmin.Remove(adminUserId);
    }
  }

  public bool IsAutoApproved(Guid adminUserId, string riskLevel)
  {
    if (!IsAutoModeEnabled(adminUserId)) return false;

    if (Enum.TryParse<RiskLevel>(riskLevel, true, out var level))
      return level <= RiskLevel.Medium;

    return false;
  }

  private void PruneExpired(DateTime now)
  {
    foreach (var adminId in _enabledUntilByAdmin.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToList())
      _enabledUntilByAdmin.Remove(adminId);
  }
}
