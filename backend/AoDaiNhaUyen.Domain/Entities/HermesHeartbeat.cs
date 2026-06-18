using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesHeartbeat : BaseEntity
{
  public required string RunnerName { get; set; }
  public required string Status { get; set; }
  public string? Model { get; set; }
  public string? GatewayStatus { get; set; }
  public int ActiveJobs { get; set; }
  public string? LastError { get; set; }
  public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
