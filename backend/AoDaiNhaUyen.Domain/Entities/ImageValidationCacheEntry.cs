namespace AoDaiNhaUyen.Domain.Entities;

public sealed class ImageValidationCacheEntry
{
  public long Id { get; set; }
  public string Sha256Hash { get; set; } = string.Empty;
  public string MimeType { get; set; } = string.Empty;
  public long FileSizeBytes { get; set; }
  public int Width { get; set; }
  public int Height { get; set; }
  public bool IsValid { get; set; }
  public string Reason { get; set; } = string.Empty;
  public string? Category { get; set; }
  public decimal? Confidence { get; set; }
  public string Provider { get; set; } = string.Empty;
  public string Model { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime ExpiresAt { get; set; }
  public DateTime? LastUsedAt { get; set; }
}
