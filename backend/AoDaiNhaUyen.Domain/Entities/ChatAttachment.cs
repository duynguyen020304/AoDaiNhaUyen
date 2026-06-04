using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ChatAttachment : BaseEntity
{
  public Guid ThreadId { get; set; }
  public Guid? MessageId { get; set; }
  public string Kind { get; set; } = "user_image";
  public string FileUrl { get; set; } = string.Empty;
  public string MimeType { get; set; } = "image/png";
  public string? OriginalFileName { get; set; }
  public long FileSizeBytes { get; set; }
  public string? MetadataJsonb { get; set; }

  public ChatThread Thread { get; set; } = null!;
  public ChatMessage? Message { get; set; }
}
