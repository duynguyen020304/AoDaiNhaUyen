using AoDaiNhaUyen.Application.DTOs.BlogPost;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class BlogImageVisibilityService(
  AppDbContext dbContext,
  IStorageService storageService,
  ILogger<BlogImageVisibilityService> logger) : IBlogImageVisibilityService
{
  private const int PrivatePresignedExpirySeconds = 86400;
  private const string PublicBlogPrefix = "aodainhauyen/public/blog";

  public async Task<string> ResolveUrlAsync(
    string objectKey,
    bool isPublic,
    string? publicObjectKey,
    CancellationToken ct = default)
  {
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

  public async Task<BlogImageVisibilityDto> MakePublicAsync(Guid blogImageId, Guid? blogPostId = null, CancellationToken ct = default)
  {
    var image = await dbContext.BlogImages.FindAsync([blogImageId], ct)
      ?? throw new InvalidOperationException($"BlogImage {blogImageId} không tồn tại.");

    if (blogPostId.HasValue && image.BlogPostId != blogPostId)
    {
      throw new InvalidOperationException($"BlogImage {blogImageId} không thuộc bài viết {blogPostId}.");
    }

    if (image.IsPublic && !string.IsNullOrWhiteSpace(image.PublicObjectKey))
    {
      var currentUrl = storageService.BuildCanonicalUrl(image.PublicObjectKey);
      return new BlogImageVisibilityDto(image.Id, true, image.PublicObjectKey, currentUrl);
    }

    var publicUrl = await storageService.CopyToPublicBlogAsync(image.ImageUrl, ct);
    var fileName = image.ImageUrl.Split('/').Last();
    var publicObjectKey = $"{PublicBlogPrefix}/{fileName}";

    image.IsPublic = true;
    image.PublicObjectKey = publicObjectKey;
    image.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(ct);

    logger.LogInformation("BlogImage {ImageId} promoted to public: {PublicKey}", image.Id, publicObjectKey);

    return new BlogImageVisibilityDto(image.Id, true, publicObjectKey, publicUrl);
  }

  public async Task<BlogImageVisibilityDto> MakePrivateAsync(Guid blogImageId, Guid? blogPostId = null, CancellationToken ct = default)
  {
    var image = await dbContext.BlogImages.FindAsync([blogImageId], ct)
      ?? throw new InvalidOperationException($"BlogImage {blogImageId} không tồn tại.");

    if (blogPostId.HasValue && image.BlogPostId != blogPostId)
    {
      throw new InvalidOperationException($"BlogImage {blogImageId} không thuộc bài viết {blogPostId}.");
    }

    if (!image.IsPublic)
    {
      var currentUrl = await storageService.GeneratePresignedGetUrlAsync(image.ImageUrl, PrivatePresignedExpirySeconds, ct);
      return new BlogImageVisibilityDto(image.Id, false, null, currentUrl);
    }

    if (!string.IsNullOrWhiteSpace(image.PublicObjectKey))
    {
      await storageService.DeleteAsync(image.PublicObjectKey, ct);
    }

    image.IsPublic = false;
    image.PublicObjectKey = null;
    image.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync(ct);

    var presignedUrl = await storageService.GeneratePresignedGetUrlAsync(image.ImageUrl, PrivatePresignedExpirySeconds, ct);

    logger.LogInformation("BlogImage {ImageId} demoted to private", image.Id);

    return new BlogImageVisibilityDto(image.Id, false, null, presignedUrl);
  }
}
