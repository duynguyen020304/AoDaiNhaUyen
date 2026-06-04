namespace AoDaiNhaUyen.Application.DTOs;

/// <summary>
/// Kết quả upload file lên S3 storage.
/// </summary>
public sealed record UploadedFileResult(
  string ObjectKey,
  string Url,
  string? PresignedUrl,
  string MimeType,
  long FileSize,
  string OriginalFileName);
