namespace AoDaiNhaUyen.Api.Configuration;

/// <summary>
/// Cấu hình S3-compatible storage (AWS S3 hoặc MinIO).
/// </summary>
public class S3StorageSettings
{
  public const string SectionName = "S3Storage";

  public string BucketName { get; set; } = string.Empty;
  public string Region { get; set; } = string.Empty;
  public string? AccessKey { get; set; }
  public string? SecretKey { get; set; }
  public string? ServiceUrl { get; set; }
  public bool UsePathStyle { get; set; }
  public int DefaultPresignedUrlExpirationInSeconds { get; set; } = 3600;
}
