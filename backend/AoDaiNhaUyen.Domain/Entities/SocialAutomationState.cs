using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class SocialAutomationState : BaseEntity
{
  public required string Key { get; set; }
  public DateTimeOffset InitializedAt { get; set; } = DateTimeOffset.UtcNow;
}
