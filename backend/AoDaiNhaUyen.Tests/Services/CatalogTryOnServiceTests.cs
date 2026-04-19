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
    var service = new CatalogTryOnService(
      dbContext,
      aiTryOnService,
      new StubHttpClientFactory(),
      new UploadStoragePathResolver(uploadRoot.Path));

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

    Assert.Equal([9, 8, 7, 6], aiTryOnService.LastRequest!.GarmentImageBytes);
    Assert.Equal("ao-dai-thu-do", aiTryOnService.LastRequest.GarmentId);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseInMemoryDatabase($"catalog-tryon-{Guid.NewGuid():N}")
      .Options;
    return new AppDbContext(options);
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

  private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken) =>
      throw new InvalidOperationException("HttpClient should not be used for file:// URIs.");
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
