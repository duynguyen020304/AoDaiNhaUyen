namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin tool risk configuration service.</summary>
public interface IAdminToolRiskService
{
  /// <summary>Get all tool risk configs.</summary>
  Task<IReadOnlyList<ToolRiskConfigDto>> GetAllAsync(CancellationToken ct = default);

  /// <summary>Update risk level for a tool.</summary>
  Task<bool> UpdateAsync(Guid id, UpdateToolRiskRequest request, CancellationToken ct = default);

  /// <summary>Seed default configs from SafetyGate if missing.</summary>
  Task SeedDefaultsAsync(CancellationToken ct = default);
}

public sealed record ToolRiskConfigDto(
  Guid Id,
  string ToolName,
  string RiskLevel,
  bool RequiresConfirmation,
  string? Description,
  string? Category);

public sealed record UpdateToolRiskRequest(
  string RiskLevel,
  bool RequiresConfirmation);
