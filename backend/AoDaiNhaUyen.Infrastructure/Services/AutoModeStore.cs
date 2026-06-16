using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>
/// In-memory per-admin store for AI autonomy mode.
/// Enabled state is permanent until the admin explicitly disables it.
/// High/Critical tools still require human confirmation.
/// </summary>
public sealed class AutoModeStore : IAutoModeStore
{
  private readonly object _lock = new();
  private readonly HashSet<Guid> _enabledAdminIds = [];

  public bool IsAutoMode
  {
    get
    {
      lock (_lock)
      {
        return _enabledAdminIds.Count > 0;
      }
    }
  }

  public bool IsAutoModeEnabled(Guid adminUserId)
  {
    lock (_lock)
    {
      return _enabledAdminIds.Contains(adminUserId);
    }
  }

  public void Enable(Guid adminUserId)
  {
    lock (_lock)
    {
      _enabledAdminIds.Add(adminUserId);
    }
  }

  public void Disable(Guid adminUserId)
  {
    lock (_lock)
    {
      _enabledAdminIds.Remove(adminUserId);
    }
  }

  public bool IsAutoApproved(Guid adminUserId, string riskLevel)
  {
    if (!IsAutoModeEnabled(adminUserId)) return false;

    if (Enum.TryParse<RiskLevel>(riskLevel, true, out var level))
      return level <= RiskLevel.Medium;

    return false;
  }
}
