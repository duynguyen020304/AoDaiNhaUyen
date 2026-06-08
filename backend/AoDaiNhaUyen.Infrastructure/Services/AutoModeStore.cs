using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Infrastructure.Services;

/// <summary>
/// In-memory store for AI autonomy mode.
/// When enabled, Medium-risk tools are auto-approved (no confirmation needed).
/// High/Critical tools still require human confirmation.
/// </summary>
public sealed class AutoModeStore : IAutoModeStore
{
  private volatile bool _isAutoMode;

  public bool IsAutoMode => _isAutoMode;

  public void Enable() => _isAutoMode = true;

  public void Disable() => _isAutoMode = false;

  public bool IsAutoApproved(string riskLevel)
  {
    if (!_isAutoMode) return false;

    // In auto mode: Medium and below are auto-approved
    if (Enum.TryParse<RiskLevel>(riskLevel, true, out var level))
      return level <= RiskLevel.Medium;

    return false;
  }
}
