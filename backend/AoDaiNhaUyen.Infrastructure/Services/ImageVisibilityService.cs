using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ImageVisibilityService(
  AppDbContext dbContext,
  IStorageService storageService,
  ILogger<ImageVisibilityService> logger) : IImageVisibilityService
{
  private const int PrivatePresignedExpirySeconds = 86400; // 24h

  public async Task<string> ResolveUrlAsync(
    string objectKey,
    bool isPublic,
    string? publicObjectKey,
    CancellationToken ct = default)
  {
    // Legacy local path — return as-is for backward compat
    if (objectKey.StartsWith("/upload/", StringComparison.OrdinalIgnoreCase))
    {
      return objectKey;
    }

    if (isPublic && !string.IsNullOrWhiteSpace(publicObjectKey))
    {
      return storageService.BuildCanonicalUrl(publicObjectKey);
    }

    return await storageService.GeneratePresignedGetUrlAsync(objectKey, PrivatePresignedExpirySeconds, ct);
  }

  public async Task<ProductImageVisibilityDto> MakePublicAsync(Guid productImageId, Guid productId, CancellationToken ct = default)
  {
    var image = await dbContext.ProductImages.FindAsync([productImageId], ct)
      ?? throw new InvalidOperationException($"ProductImage {productImageId} không tồn tại.");

    if (image.ProductId != productId)
    {
      throw new InvalidOperationException($"ProductImage {productImageId} không thuộc sản phẩm {productId}.");
    }

    if (image.IsPublic)
    {
      var url = storageService.BuildCanonicalUrl(image.PublicObjectKey!);
      return new ProductImageVisibilityDto(image.Id, true, image.PublicObjectKey, url);
    }

    // Copy to public prefix via S3
    var publicUrl = await storageService.CopyToPublicAsync(image.ImageUrl, ct);

    // Extract public object key from canonical URL structure
    var fileName = image.ImageUrl.Split('/').Last();
    var publicObjectKey = $"aodainhauyen/public/products/{fileName}";

    image.IsPublic = true;
    image.PublicObjectKey = publicObjectKey;
    image.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(ct);

    logger.LogInformation("ProductImage {ImageId} promoted to public: {PublicKey}", image.Id, publicObjectKey);

    return new ProductImageVisibilityDto(image.Id, true, publicObjectKey, publicUrl);
  }

  public async Task<ProductImageVisibilityDto> MakePrivateAsync(Guid productImageId, Guid productId, CancellationToken ct = default)
  {
    var image = await dbContext.ProductImages.FindAsync([productImageId], ct)
      ?? throw new InvalidOperationException($"ProductImage {productImageId} không tồn tại.");

    if (image.ProductId != productId)
    {
      throw new InvalidOperationException($"ProductImage {productImageId} không thuộc sản phẩm {productId}.");
    }

    if (!image.IsPublic)
    {
      var url = await storageService.GeneratePresignedGetUrlAsync(image.ImageUrl, PrivatePresignedExpirySeconds, ct);
      return new ProductImageVisibilityDto(image.Id, false, null, url);
    }

    // Delete public copy from S3
    if (!string.IsNullOrWhiteSpace(image.PublicObjectKey))
    {
      await storageService.DeleteAsync(image.PublicObjectKey, ct);
    }

    image.IsPublic = false;
    image.PublicObjectKey = null;
    image.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(ct);

    var presignedUrl = await storageService.GeneratePresignedGetUrlAsync(image.ImageUrl, PrivatePresignedExpirySeconds, ct);

    logger.LogInformation("ProductImage {ImageId} demoted to private", image.Id);

    return new ProductImageVisibilityDto(image.Id, false, null, presignedUrl);
  }
}
