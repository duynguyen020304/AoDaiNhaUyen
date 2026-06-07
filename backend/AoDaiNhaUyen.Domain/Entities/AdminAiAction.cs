using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class AdminAiAction : BaseEntity
{
  public Guid AdminUserId { get; set; }
  public User AdminUser { get; set; } = null!;
  public AdminAiActionType ActionType { get; set; }
  public RiskLevel RiskLevel { get; set; }
  public required string ToolName { get; set; }
  public string? ToolInput { get; set; }
  public string? ToolResult { get; set; }
  public bool Success { get; set; }
  public string? ConfirmedBy { get; set; }
  public DateTime? ConfirmedAt { get; set; }
}
