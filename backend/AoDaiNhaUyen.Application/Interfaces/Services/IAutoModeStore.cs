namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>In-memory store for AI autonomy mode state.</summary>
public interface IAutoModeStore
{
  bool IsAutoMode { get; }
  void Enable();
  void Disable();
  bool IsAutoApproved(string riskLevel);
}
