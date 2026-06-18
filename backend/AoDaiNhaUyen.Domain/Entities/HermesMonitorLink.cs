using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesMonitorLink : BaseEntity
{
  public required string TokenHash { get; set; }
  public string ScopeType { get; set; } = "event";
  public required string ScopeId { get; set; }
  public Guid? CreatedByAdminUserId { get; set; }
  public DateTimeOffset ExpiresAt { get; set; }
  public DateTimeOffset? RevokedAt { get; set; }
  public DateTimeOffset? LastAccessedAt { get; set; }
  public int AccessCount { get; set; }
}
