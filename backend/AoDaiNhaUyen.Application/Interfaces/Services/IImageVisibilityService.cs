using AoDaiNhaUyen.Application.DTOs;

namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>
/// Resolves product image URLs based on public/private visibility.
/// Manages promoting/demoting images between S3 private and public prefixes.
/// </summary>
public interface IImageVisibilityService
{
  /// <summary>
  /// Resolve the accessible URL for a product image.
  /// Public images return canonical URL; private images return presigned URL (24h).
  /// </summary>
  Task<string> ResolveUrlAsync(string objectKey, bool isPublic, string? publicObjectKey, CancellationToken ct = default);

  /// <summary>
  /// Promote a product image to public: copy S3 object to public prefix, update DB.
  /// </summary>
  Task<ProductImageVisibilityDto> MakePublicAsync(Guid productImageId, Guid productId, CancellationToken ct = default);

  /// <summary>
  /// Demote a product image to private: delete public S3 object, update DB.
  /// </summary>
  Task<ProductImageVisibilityDto> MakePrivateAsync(Guid productImageId, Guid productId, CancellationToken ct = default);
}
