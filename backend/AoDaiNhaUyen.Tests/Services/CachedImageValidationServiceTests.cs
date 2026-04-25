using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Configuration;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class CachedImageValidationServiceTests
{
  [Fact]
  public async Task ValidatePersonImageAsync_CacheMissValid_CallsModelAndStoresCache()
  {
    await using var dbContext = CreateDbContext();
    var model = new StubImageValidationService(new ImageValidationResultDto(true, "Ảnh phù hợp.", "valid_person_face", 0.95m));
    var service = CreateService(dbContext, model);
    var bytes = CreateImageBytes();

    var result = await service.ValidatePersonImageAsync(bytes, "image/png", "person.png");

    Assert.True(result.IsValid);
    Assert.Equal(1, model.CallCount);
    var entry = await dbContext.ImageValidationCacheEntries.SingleAsync();
    Assert.True(entry.IsValid);
    Assert.Equal("valid_person_face", entry.Category);
    Assert.True(entry.ExpiresAt > DateTime.UtcNow.AddDays(6));
  }

  [Fact]
  public async Task ValidatePersonImageAsync_CacheHitValid_SkipsModel()
  {
    await using var dbContext = CreateDbContext();
    var bytes = CreateImageBytes();
    dbContext.ImageValidationCacheEntries.Add(CreateCacheEntry(bytes, true, "Ảnh phù hợp.", DateTime.UtcNow.AddDays(1)));
    await dbContext.SaveChangesAsync();
    var model = new StubImageValidationService(new ImageValidationResultDto(false, "Model should not be called."));
    var service = CreateService(dbContext, model);

    var result = await service.ValidatePersonImageAsync(bytes, "image/png", "person.png");

    Assert.True(result.IsValid);
    Assert.Equal(0, model.CallCount);
    Assert.NotNull((await dbContext.ImageValidationCacheEntries.SingleAsync()).LastUsedAt);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_CacheHitInvalid_SkipsModelAndReturnsReason()
  {
    await using var dbContext = CreateDbContext();
    var bytes = CreateImageBytes();
    dbContext.ImageValidationCacheEntries.Add(CreateCacheEntry(bytes, false, "Ảnh không có người.", DateTime.UtcNow.AddDays(1)));
    await dbContext.SaveChangesAsync();
    var model = new StubImageValidationService(new ImageValidationResultDto(true, "Model should not be called."));
    var service = CreateService(dbContext, model);

    var result = await service.ValidatePersonImageAsync(bytes, "image/png", "person.png");

    Assert.False(result.IsValid);
    Assert.Equal("Ảnh không có người.", result.Reason);
    Assert.Equal(0, model.CallCount);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_ExpiredCache_CallsModelAndRefreshesCache()
  {
    await using var dbContext = CreateDbContext();
    var bytes = CreateImageBytes();
    dbContext.ImageValidationCacheEntries.Add(CreateCacheEntry(bytes, false, "expired", DateTime.UtcNow.AddDays(-1)));
    await dbContext.SaveChangesAsync();
    var model = new StubImageValidationService(new ImageValidationResultDto(true, "Ảnh phù hợp.", "valid_person_face", 0.9m));
    var service = CreateService(dbContext, model);

    var result = await service.ValidatePersonImageAsync(bytes, "image/png", "person.png");

    Assert.True(result.IsValid);
    Assert.Equal(1, model.CallCount);
    var entry = await dbContext.ImageValidationCacheEntries.SingleAsync();
    Assert.True(entry.IsValid);
    Assert.True(entry.ExpiresAt > DateTime.UtcNow);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_InvalidExtension_DoesNotCallModelOrCache()
  {
    await using var dbContext = CreateDbContext();
    var model = new StubImageValidationService(new ImageValidationResultDto(true, "Model should not be called."));
    var service = CreateService(dbContext, model);

    var result = await service.ValidatePersonImageAsync(CreateImageBytes(), "image/png", "person.gif");

    Assert.False(result.IsValid);
    Assert.Equal(0, model.CallCount);
    Assert.Empty(dbContext.ImageValidationCacheEntries);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_TinyImage_DoesNotCallModelOrCache()
  {
    await using var dbContext = CreateDbContext();
    var model = new StubImageValidationService(new ImageValidationResultDto(true, "Model should not be called."));
    var service = CreateService(dbContext, model, new ImageValidationOptions { MinWidth = 64, MinHeight = 64 });

    var result = await service.ValidatePersonImageAsync(CreateImageBytes(32, 32), "image/png", "person.png");

    Assert.False(result.IsValid);
    Assert.Equal(0, model.CallCount);
    Assert.Empty(dbContext.ImageValidationCacheEntries);
  }

  [Fact]
  public async Task ValidatePersonImageAsync_SameBytes_ReusesCache()
  {
    await using var dbContext = CreateDbContext();
    var model = new StubImageValidationService(new ImageValidationResultDto(true, "Ảnh phù hợp.", "valid_person_face", 0.95m));
    var service = CreateService(dbContext, model);
    var bytes = CreateImageBytes();

    await service.ValidatePersonImageAsync(bytes, "image/png", "one.png");
    await service.ValidatePersonImageAsync(bytes, "image/png", "two.png");

    Assert.Equal(1, model.CallCount);
    Assert.Equal(1, await dbContext.ImageValidationCacheEntries.CountAsync());
  }

  private static CachedImageValidationService CreateService(
    AppDbContext dbContext,
    IImageValidationService model,
    ImageValidationOptions? options = null) =>
    new(
      dbContext,
      model,
      Options.Create(options ?? new ImageValidationOptions()),
      Options.Create(new GoogleCloudOptions { ImageValidationModel = "gemini-validation-test" }));

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"image-validation-cache-{Guid.NewGuid():N}")
      .Options;
    return new AppDbContext(options);
  }

  private static byte[] CreateImageBytes(int width = 128, int height = 128)
  {
    using var image = new Image<Rgba32>(width, height);
    using var stream = new MemoryStream();
    image.SaveAsPng(stream);
    return stream.ToArray();
  }

  private static ImageValidationCacheEntry CreateCacheEntry(byte[] bytes, bool isValid, string reason, DateTime expiresAt) =>
    new()
    {
      Sha256Hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant(),
      MimeType = "image/png",
      FileSizeBytes = bytes.Length,
      Width = 128,
      Height = 128,
      IsValid = isValid,
      Reason = reason,
      Category = isValid ? "valid_person_face" : "object_only",
      Provider = "vertex-ai",
      Model = "gemini-validation-test",
      CreatedAt = DateTime.UtcNow.AddHours(-1),
      ExpiresAt = expiresAt
    };

  private sealed class StubImageValidationService(ImageValidationResultDto result) : IImageValidationService
  {
    public int CallCount { get; private set; }

    public Task<ImageValidationResultDto> ValidatePersonImageAsync(
      byte[] imageBytes,
      string mimeType,
      CancellationToken cancellationToken = default)
    {
      CallCount++;
      return Task.FromResult(result);
    }
  }
}
