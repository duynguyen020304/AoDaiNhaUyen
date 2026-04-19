namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ChatAttachment
{
  public long Id { get; set; }
  public long ThreadId { get; set; }
  public long? MessageId { get; set; }
  public string Kind { get; set; } = "user_image";
  public string FileUrl { get; set; } = string.Empty;
  public string MimeType { get; set; } = "image/png";
  public string? OriginalFileName { get; set; }
  public long FileSizeBytes { get; set; }
  public string? MetadataJsonb { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public ChatThread Thread { get; set; } = null!;
  public ChatMessage? Message { get; set; }
}
