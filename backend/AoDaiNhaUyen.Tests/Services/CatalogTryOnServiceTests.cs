using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using AoDaiNhaUyen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AoDaiNhaUyen.Tests.Services;

public sealed class CatalogTryOnServiceTests
{
  [Fact]
  public async Task CreateAsync_ReadsGarmentImageFromFileUri()
  {
    await using var dbContext = CreateDbContext();
    using var uploadRoot = new TemporaryDirectory();

    var category = new Category
    {
      Name = "Áo dài",
      Slug = "ao-dai",
      IsActive = true
    };

    var product = new Product
    {
      Category = category,
      Name = "Áo dài thử đồ",
      Slug = "ao-dai-thu-do",
      ProductType = "ao_dai",
      Status = "active"
    };

    var garmentPath = Path.Combine(uploadRoot.Path, "tryon", "ao-dai-thu-do.png");
    Directory.CreateDirectory(Path.GetDirectoryName(garmentPath)!);
    await File.WriteAllBytesAsync(garmentPath, [9, 8, 7, 6]);

    product.AiAssets.Add(new ProductAiAsset
    {
      AssetKind = "tryon_garment",
      FileUrl = new Uri(garmentPath).AbsoluteUri,
      MimeType = "image/png",
      IsActive = true
    });

    dbContext.Products.Add(product);
    await dbContext.SaveChangesAsync();

    var aiTryOnService = new CapturingAiTryOnService();
    var imageValidationService = new StubImageValidationService(new ImageValidationResultDto(true, "Ảnh phù hợp để thử đồ.", "valid_person", 0.95m));
    var service = new CatalogTryOnService(
      dbContext,
      aiTryOnService,
      imageValidationService,
      new StubHttpClientFactory(),
      new UploadStoragePathResolver(uploadRoot.Path),
      new StubStorageService(),
      new StubImageVisibilityService());

    await service.CreateAsync(
      new CatalogAiTryOnRequestDto(
        null,
        [1, 2, 3],
        "image/png",
        product.Id,
        null,
        [],
        null,
        null,
        []),
      CancellationToken.None);

    Assert.Equal([1, 2, 3], imageValidationService.LastBytes);
    Assert.Equal("image/png", imageValidationService.LastMimeType);
    Assert.Equal([9, 8, 7, 6], aiTryOnService.LastRequest!.GarmentImageBytes);
    Assert.Equal("ao-dai-thu-do", aiTryOnService.LastRequest.GarmentId);
  }

  [Fact]
  public async Task CreateAsync_InvalidPersonImage_ThrowsAndDoesNotGenerate()
  {
    await using var dbContext = CreateDbContext();
    using var uploadRoot = new TemporaryDirectory();

    var aiTryOnService = new CapturingAiTryOnService();
    var service = new CatalogTryOnService(
      dbContext,
      aiTryOnService,
      new StubImageValidationService(new ImageValidationResultDto(false, "Ảnh không có người phù hợp để thử đồ.", "object_only", 0.9m)),
      new StubHttpClientFactory(),
      new UploadStoragePathResolver(uploadRoot.Path),
      new StubStorageService(),
      new StubImageVisibilityService());

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
      new CatalogAiTryOnRequestDto(
        null,
        [1, 2, 3],
        "image/png",
        Guid.NewGuid(),
        null,
        [],
        null,
        null,
        []),
      CancellationToken.None));

    Assert.Equal("Ảnh không có người phù hợp để thử đồ.", exception.Message);
    Assert.Null(aiTryOnService.LastRequest);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"catalog-tryon-{Guid.NewGuid():N}")
      .Options;
    return new AppDbContext(options);
  }

  private sealed class StubImageValidationService(ImageValidationResultDto result) : ICachedImageValidationService
  {
    public byte[]? LastBytes { get; private set; }
    public string? LastMimeType { get; private set; }

    public Task<ImageValidationResultDto> ValidatePersonImageAsync(
      byte[] imageBytes,
      string mimeType,
      string? fileName = null,
      CancellationToken cancellationToken = default)
    {
      LastBytes = imageBytes;
      LastMimeType = mimeType;
      return Task.FromResult(result);
    }
  }

  private sealed class CapturingAiTryOnService : IAiTryOnService
  {
    public AiTryOnRequestDto? LastRequest { get; private set; }

    public Task<AiTryOnResultDto> GenerateAsync(
      AiTryOnRequestDto request,
      CancellationToken cancellationToken = default)
    {
      LastRequest = request;
      return Task.FromResult(new AiTryOnResultDto("data:image/png;base64,AQID", "image/png"));
    }
  }

  private sealed class StubHttpClientFactory : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) =>
      new(new ThrowingHttpMessageHandler());
  }

  private sealed class StubStorageService : IStorageService
  {
    public Task<UploadedFileResult> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null, CancellationToken ct = default) =>
      Task.FromResult(new UploadedFileResult($"stub-{Guid.NewGuid():N}", "https://stub.example/o", null, contentType, stream.Length, fileName));

    public Task<string> GeneratePresignedGetUrlAsync(string objectKey, int expirationSeconds = 3600, CancellationToken ct = default) =>
      Task.FromResult($"https://presigned.stub/{objectKey}");

    public Task DeleteAsync(string objectKey, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
    public Task<bool> ExistsAsync(string objectKey, CancellationToken ct = default) => Task.FromResult(false);
    public string BuildCanonicalUrl(string objectKey) => $"https://canonical.stub/{objectKey}";    public Task PutObjectWithKeyAsync(string objectKey, Stream stream, string contentType, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> CopyToPublicAsync(string objectKey, CancellationToken ct = default) => Task.FromResult($"https://canonical.stub/{objectKey}");
    public bool IsConfigured() => true;
  }

  private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken) =>
      throw new InvalidOperationException("HttpClient should not be used for file:// URIs.");
  }

  private sealed class StubImageVisibilityService : IImageVisibilityService
  {
    public Task<ProductImageVisibilityDto> MakePrivateAsync(Guid productImageId, Guid productId, CancellationToken ct = default)
      => Task.FromResult(new ProductImageVisibilityDto(productImageId, false, null, "url"));

    public Task<ProductImageVisibilityDto> MakePublicAsync(Guid productImageId, Guid productId, CancellationToken ct = default)
      => Task.FromResult(new ProductImageVisibilityDto(productImageId, true, "pub", "url"));

    public Task<string> ResolveUrlAsync(string objectKey, bool isPublic, string? publicObjectKey, CancellationToken ct = default)
      => Task.FromResult("https://stub.example/url");
  }

  private sealed class TemporaryDirectory : IDisposable
  {
    public TemporaryDirectory()
    {
      Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"aodai-catalog-tryon-{Guid.NewGuid():N}");
      Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
      if (Directory.Exists(Path))
      {
        Directory.Delete(Path, recursive: true);
      }
    }
  }
}
