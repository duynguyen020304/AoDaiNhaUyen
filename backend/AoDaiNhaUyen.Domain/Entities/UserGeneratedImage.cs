using AoDaiNhaUyen.Domain.Common;

namespace AoDaiNhaUyen.Domain.Entities;

/// <summary>
/// Ảnh do người dùng tạo (upload trong chat hoặc AI try-on).
/// </summary>
public sealed class UserGeneratedImage : BaseEntity
{
  public Guid? UserId { get; set; }
  public string? GuestKeyHash { get; set; }
  public string ObjectKey { get; set; } = string.Empty;
  public string Url { get; set; } = string.Empty;
  public string Kind { get; set; } = "user_image";
  public string MimeType { get; set; } = "image/png";
  public string? OriginalFileName { get; set; }
  public long FileSizeBytes { get; set; }
  public string SourceType { get; set; } = "chat";

  public User? User { get; set; }
}
