using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class EmailSendLog : BaseEntity
{
  public Guid EmailJobId { get; set; }
  public required string ToEmail { get; set; }
  public required string TemplateKey { get; set; }
  public required string Status { get; set; }
  public string? ProviderMessageId { get; set; }
  public DateTime? SentAt { get; set; }
  public DateTime? FailedAt { get; set; }
  public string? ErrorMessage { get; set; }

  public EmailJob EmailJob { get; set; } = null!;
}
