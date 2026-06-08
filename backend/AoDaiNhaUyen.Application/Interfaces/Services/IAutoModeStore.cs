namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>In-memory store for AI autonomy mode state.</summary>
public interface IAutoModeStore
{
  bool IsAutoMode { get; }
  bool IsAutoModeEnabled(Guid adminUserId);
  void Enable(Guid adminUserId);
  void Disable(Guid adminUserId);
  bool IsAutoApproved(Guid adminUserId, string riskLevel);
}
