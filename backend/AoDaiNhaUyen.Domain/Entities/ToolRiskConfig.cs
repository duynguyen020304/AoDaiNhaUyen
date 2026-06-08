namespace AoDaiNhaUyen.Domain.Entities;

/// <summary>
/// Configurable risk level for each AI agent tool.
/// Allows admin to override default safety gate behavior.
/// </summary>
public sealed class ToolRiskConfig
{
  public Guid Id { get; set; } = Guid.NewGuid();

  /// <summary>Tool name (must match tool definition in AdminAgentService).</summary>
  public string ToolName { get; set; } = string.Empty;

  /// <summary>Risk level: Read, Low, Medium, High, Critical.</summary>
  public string RiskLevel { get; set; } = "Medium";

  /// <summary>Whether this tool requires human confirmation before execution.</summary>
  public bool RequiresConfirmation { get; set; } = true;

  /// <summary>Human-readable description of what this tool does.</summary>
  public string? Description { get; set; }

  /// <summary>Tool category for grouping in UI.</summary>
  public string? Category { get; set; }

  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
