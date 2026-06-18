using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class HermesRun : BaseEntity
{
  public string Status { get; set; } = "running";
  public string Trigger { get; set; } = "admin_chat";
  public Guid? AdminUserId { get; set; }
  public string? ConversationId { get; set; }
  public required string PromptPreview { get; set; }
  public string? ResultPreview { get; set; }
  public string? Error { get; set; }
  public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? CompletedAt { get; set; }

  public ICollection<HermesReport> Reports { get; set; } = new List<HermesReport>();
}
