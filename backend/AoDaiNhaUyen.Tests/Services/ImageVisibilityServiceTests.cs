using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class ImageVisibilityServiceTests
{
  [Fact]
  public void xUnit_SuppressCollectionSize()
  {
    Assert.Single(new[] { 1 });
  }

  [Fact]
  public async Task ResolveUrl_LegacyPath_ReturnsAsIs()
  {
    var service = CreateService(CreateDbContext());
    var result = await service.ResolveUrlAsync("/upload/products/test.webp", false, null);
    Assert.Equal("/upload/products/test.webp", result);
  }

  [Fact]
  public async Task ResolveUrl_PublicImage_ReturnsCanonicalUrl()
  {
    var storage = new TrackingStorageService();
    var service = CreateService(CreateDbContext(), storage);
    var result = await service.ResolveUrlAsync(
      "aodainhauyen/private/products/test.webp", true, "aodainhauyen/public/products/test.webp");

    Assert.Equal("https://canonical.stub/aodainhauyen/public/products/test.webp", result);
    Assert.Equal(1, storage.BuildCanonicalUrlCalls);
  }

  [Fact]
  public async Task ResolveUrl_PrivateImage_ReturnsPresignedUrl()
  {
    var storage = new TrackingStorageService();
    var service = CreateService(CreateDbContext(), storage);
    var result = await service.ResolveUrlAsync("aodainhauyen/private/products/test.webp", false, null);

    Assert.StartsWith("https://presigned.stub/", result);
    Assert.Equal(1, storage.PresignedUrlCalls);
  }

  [Fact]
  public async Task MakePublicAsync_WrongProductId_ThrowsInvalidOperationException()
  {
    await using var db = CreateDbContext();
    var productId = Guid.NewGuid();
    var otherProductId = Guid.NewGuid();
    var imageId = Guid.NewGuid();

    db.ProductImages.Add(new ProductImage
    {
      Id = imageId,
      ProductId = productId,
      ImageUrl = "aodainhauyen/private/products/test.webp",
      SortOrder = 0,
      IsPrimary = true
    });
    await db.SaveChangesAsync();

    var service = CreateService(db);
    var ex = await Assert.ThrowsAsync<InvalidOperationException>(
      () => service.MakePublicAsync(imageId, otherProductId));
    Assert.Contains("không thuộc sản phẩm", ex.Message);
  }

  [Fact]
  public async Task MakePublicAsync_Success_CopiesToPublic()
  {
    await using var db = CreateDbContext();
    var productId = Guid.NewGuid();
    var imageId = Guid.NewGuid();

    db.ProductImages.Add(new ProductImage
    {
      Id = imageId,
      ProductId = productId,
      ImageUrl = "aodainhauyen/private/products/test.webp",
      SortOrder = 0,
      IsPrimary = true
    });
    await db.SaveChangesAsync();

    var service = CreateService(db);
    var result = await service.MakePublicAsync(imageId, productId);

    Assert.True(result.IsPublic);
    Assert.Equal("aodainhauyen/public/products/test.webp", result.PublicObjectKey);

    var updated = await db.ProductImages.FindAsync(imageId);
    Assert.True(updated!.IsPublic);
    Assert.Equal("aodainhauyen/public/products/test.webp", updated.PublicObjectKey);
  }

  [Fact]
  public async Task MakePrivateAsync_Success_DeletesPublic()
  {
    await using var db = CreateDbContext();
    var productId = Guid.NewGuid();
    var imageId = Guid.NewGuid();

    db.ProductImages.Add(new ProductImage
    {
      Id = imageId,
      ProductId = productId,
      ImageUrl = "aodainhauyen/private/products/test.webp",
      SortOrder = 0,
      IsPrimary = true,
      IsPublic = true,
      PublicObjectKey = "aodainhauyen/public/products/test.webp"
    });
    await db.SaveChangesAsync();

    var storage = new TrackingStorageService();
    var service = CreateService(db, storage);
    var result = await service.MakePrivateAsync(imageId, productId);

    Assert.False(result.IsPublic);
    Assert.Null(result.PublicObjectKey);

    var updated = await db.ProductImages.FindAsync(imageId);
    Assert.False(updated!.IsPublic);
    Assert.Null(updated.PublicObjectKey);
    Assert.Single(storage.DeletedKeys);
  }

  private static ImageVisibilityService CreateService(AppDbContext db, IStorageService? storageService = null)
  {
    return new ImageVisibilityService(db, storageService ?? new TrackingStorageService(), NullLogger<ImageVisibilityService>.Instance);
  }

  private static AppDbContext CreateDbContext()
  {
    return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
  }

  private sealed class TrackingStorageService : IStorageService
  {
    public int BuildCanonicalUrlCalls;
    public int PresignedUrlCalls;
    public readonly List<string> DeletedKeys = new();

    public Task<UploadedFileResult> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken ct = default) =>
      Task.FromResult(new UploadedFileResult($"key-{Guid.NewGuid():N}", "https://stub.example/o", null, contentType, stream.Length, fileName));

    public Task<string> GeneratePresignedGetUrlAsync(string objectKey, int expirationSeconds = 3600, CancellationToken ct = default)
    {
      PresignedUrlCalls++;
      return Task.FromResult($"https://presigned.stub/{objectKey}?expires={expirationSeconds}");
    }

    public Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
      DeletedKeys.Add(objectKey);
      return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) =>
      Task.FromResult<Stream>(new MemoryStream());

    public Task PutObjectWithKeyAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default) =>
      Task.CompletedTask;

    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) =>
      Task.FromResult(false);

    public string BuildCanonicalUrl(string objectKey)
    {
      BuildCanonicalUrlCalls++;
      return $"https://canonical.stub/{objectKey}";
    }

    public Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default)
    {
      var fileName = objectKey.Split('/').Last();
      return Task.FromResult($"https://canonical.stub/aodainhauyen/public/products/{fileName}");
    }

    public bool IsConfigured() => true;
  }
}
