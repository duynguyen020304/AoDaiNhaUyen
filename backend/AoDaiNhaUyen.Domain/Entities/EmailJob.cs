using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class EmailJob : BaseEntity
{
  public required string ToEmail { get; set; }
  public required string TemplateKey { get; set; }
  public required string PayloadJson { get; set; }
  public string Status { get; set; } = "queued";
  public int RetryCount { get; set; }
  public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
  public DateTime? SentAt { get; set; }
  public string? ErrorMessage { get; set; }
  public string? ProviderMessageId { get; set; }

  public ICollection<EmailSendLog> SendLogs { get; set; } = new List<EmailSendLog>();
}
