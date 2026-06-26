using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class AdminAuditLog : BaseEntity
{
  public Guid? ActorUserId { get; set; }
  public User? ActorUser { get; set; }
  public string? ActorName { get; set; }
  public string? ActorEmail { get; set; }
  public string? ActorRoles { get; set; }
  public string HttpMethod { get; set; } = string.Empty;
  public string Path { get; set; } = string.Empty;
  public string? QueryString { get; set; }
  public string? ControllerName { get; set; }
  public string? ActionName { get; set; }
  public string ActionType { get; set; } = string.Empty;
  public string EntityType { get; set; } = string.Empty;
  public string? EntityId { get; set; }
  public int StatusCode { get; set; }
  public bool Success { get; set; }
  public string? RequestPreview { get; set; }
  public string? ResponsePreview { get; set; }
  public string? Error { get; set; }
  public string? IpAddressHash { get; set; }
  public string? UserAgentHash { get; set; }
}
