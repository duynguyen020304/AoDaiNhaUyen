using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class MarketingConsent : BaseEntity
{
  public Guid? UserId { get; set; }
  public Guid SubscriberId { get; set; }
  public string Channel { get; set; } = "email";
  public bool IsOptIn { get; set; }
  public required string Source { get; set; }
  public string ConsentVersion { get; set; } = "2026-01";
  public DateTime? ConsentedAt { get; set; }
  public DateTime? RevokedAt { get; set; }
  public string? IpHash { get; set; }
  public string? UserAgentHash { get; set; }

  public User? User { get; set; }
  public Subscriber Subscriber { get; set; } = null!;
}
