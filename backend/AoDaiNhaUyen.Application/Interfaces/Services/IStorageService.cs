using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>
/// Abstraction for file storage (S3/MinIO).
/// </summary>
public interface IStorageService
{
  /// <summary>
  /// Upload file to S3.
  /// </summary>
  Task<UploadedFileResult> UploadAsync(
    Stream stream,
    string fileName,
    string contentType,
    string? folder = null,
    CancellationToken ct = default);

  /// <summary>
  /// Generate time-limited presigned GET URL for an object.
  /// </summary>
  Task<string> GeneratePresignedGetUrlAsync(
    string objectKey,
    int expirationSeconds = 3600,
    CancellationToken ct = default);

  /// <summary>
  /// Delete an object from S3.
  /// </summary>
  Task DeleteAsync(
    string objectKey,
    CancellationToken ct = default);

  /// <summary>
  /// Download an object from S3 as a stream.
  /// </summary>
  Task<Stream> DownloadAsync(
    string objectKey,
    CancellationToken ct = default);
  /// <summary>
  /// Upload file to S3 at a specific object key (no GUID prefix).
  /// Used for deterministic seed keys.
  /// </summary>
  Task PutObjectWithKeyAsync(
    string objectKey,
    Stream stream,
    string contentType,
    CancellationToken ct = default);

  /// <summary>
  /// Check whether an object exists in S3.
  /// </summary>
  Task<bool> ExistsAsync(
    string objectKey,
    CancellationToken ct = default);


  /// <summary>
  /// Build public canonical URL for an object key.
  /// </summary>
  string BuildCanonicalUrl(string objectKey);

  /// <summary>
  /// Copy object from private prefix to public products prefix (keep both).
  /// Returns canonical URL of public object.
  /// </summary>
  Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default);

  /// <summary>
  /// Copy object from private prefix to public blog prefix (keep both).
  /// Returns canonical URL of public object.
  /// </summary>
  Task<string> CopyToPublicBlogAsync(string objectKey, CancellationToken ct = default)
    => CopyToPublicAsync(objectKey, ct);

  /// <summary>
  /// Check whether S3 is properly configured (BucketName + Region/ServiceUrl).
  /// </summary>
  bool IsConfigured();
}
