using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CatalogTryOnService(
  AppDbContext dbContext,
  IAiTryOnService aiTryOnService,
  IHttpClientFactory httpClientFactory,
  IUploadStoragePathResolver uploadStoragePathResolver) : ICatalogTryOnService
{
  private const string CuratedGarmentAssetKind = "tryon_garment_curated";
  private const string GarmentAssetKind = "tryon_garment";
  private const string CuratedAccessoryAssetKind = "tryon_accessory_curated";
  private const string AccessoryAssetKind = "tryon_accessory";

  public async Task<AiTryOnCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default)
  {
    var products = await dbContext.Products
      .AsNoTracking()
      .Include(product => product.Category)
      .Include(product => product.Variants)
      .Include(product => product.Images)
      .Include(product => product.AiAssets)
      .Where(product => product.Status == "active")
      .Where(product => product.AiAssets.Any(asset => asset.IsActive))
      .OrderByDescending(product => product.IsFeatured)
      .ThenBy(product => product.Name)
      .ToListAsync(cancellationToken);

    var garments = products
      .Where(product => product.ProductType == "ao_dai")
      .Select(product => MapCatalogItem(product, GarmentAssetKind))
      .Where(item => item is not null)
      .Cast<AiTryOnCatalogItemDto>()
      .ToList();

    var accessories = products
      .Where(product => product.ProductType == "phu_kien")
      .Select(product => MapCatalogItem(product, AccessoryAssetKind))
      .Where(item => item is not null)
      .Cast<AiTryOnCatalogItemDto>()
      .ToList();

    return new AiTryOnCatalogDto(garments, accessories);
  }

  public async Task<AiTryOnResultDto> CreateAsync(
    CatalogAiTryOnRequestDto request,
    CancellationToken cancellationToken = default)
  {
    var garmentSelection = await ResolveGarmentSelectionAsync(request, cancellationToken);
    var accessorySelections = request.AccessoryProductIds.Count > 0
      ? await ResolveAccessorySelectionsAsync(request.AccessoryProductIds, cancellationToken)
      : request.LegacyAccessoryImages;

    var aiRequest = new AiTryOnRequestDto(
      garmentSelection.GarmentId,
      request.PersonImageBytes,
      request.PersonImageMimeType,
      garmentSelection.ImageBytes,
      garmentSelection.MimeType,
      accessorySelections);

    return await aiTryOnService.GenerateAsync(aiRequest, cancellationToken);
  }

  private static AiTryOnCatalogItemDto? MapCatalogItem(Product product, string assetKind)
  {
    var aiAsset = SelectAiAsset(product, assetKind);
    if (aiAsset is null)
    {
      return null;
    }

    var thumbnailUrl = product.Images
      .OrderBy(image => image.SortOrder)
      .FirstOrDefault(image => image.IsPrimary)?.ImageUrl
      ?? product.Images.OrderBy(image => image.SortOrder).FirstOrDefault()?.ImageUrl
      ?? aiAsset.FileUrl;

    var defaultVariantId = product.Variants
      .OrderByDescending(variant => variant.IsDefault)
      .ThenBy(variant => variant.Id)
      .Select(variant => (long?)variant.Id)
      .FirstOrDefault();

    return new AiTryOnCatalogItemDto(
      product.Id,
      defaultVariantId,
      product.Name,
      product.ProductType,
      product.Category.Slug,
      thumbnailUrl,
      aiAsset.FileUrl,
      product.IsFeatured);
  }

  private async Task<ResolvedTryOnImage> ResolveGarmentSelectionAsync(
    CatalogAiTryOnRequestDto request,
    CancellationToken cancellationToken)
  {
    if (request.GarmentProductId.HasValue)
    {
      var product = await dbContext.Products
        .AsNoTracking()
        .Include(item => item.AiAssets)
        .FirstOrDefaultAsync(
          item => item.Id == request.GarmentProductId.Value && item.Status == "active",
          cancellationToken);

      if (product is null)
      {
        throw new InvalidOperationException("Không tìm thấy trang phục đã chọn.");
      }

      var aiAsset = SelectAiAsset(product, GarmentAssetKind, request.GarmentVariantId);
      if (aiAsset is null)
      {
        throw new InvalidOperationException("Trang phục đã chọn chưa có AI asset đang hoạt động.");
      }

      return new ResolvedTryOnImage(
        product.Slug,
        await ReadAssetBytesAsync(aiAsset.FileUrl, cancellationToken),
        aiAsset.MimeType);
    }

    if (request.LegacyGarmentImageBytes is null || string.IsNullOrWhiteSpace(request.LegacyGarmentImageMimeType))
    {
      throw new InvalidOperationException("Garment selection is required.");
    }

    return new ResolvedTryOnImage(
      request.LegacyGarmentId?.Trim() ?? "legacy-garment",
      request.LegacyGarmentImageBytes,
      request.LegacyGarmentImageMimeType);
  }

  private async Task<IReadOnlyList<AiTryOnAccessoryImageDto>> ResolveAccessorySelectionsAsync(
    IReadOnlyList<long> accessoryProductIds,
    CancellationToken cancellationToken)
  {
    if (accessoryProductIds.Count == 0)
    {
      return [];
    }

    var products = await dbContext.Products
      .AsNoTracking()
      .Include(product => product.AiAssets)
      .Where(product => accessoryProductIds.Contains(product.Id) && product.Status == "active")
      .ToListAsync(cancellationToken);

    var productById = products.ToDictionary(product => product.Id);
    var resolved = new List<AiTryOnAccessoryImageDto>(accessoryProductIds.Count);

    foreach (var productId in accessoryProductIds)
    {
      if (!productById.TryGetValue(productId, out var product))
      {
        throw new InvalidOperationException("Không tìm thấy phụ kiện đã chọn.");
      }

      var aiAsset = SelectAiAsset(product, AccessoryAssetKind);
      if (aiAsset is null)
      {
        throw new InvalidOperationException($"Phụ kiện \"{product.Name}\" chưa có AI asset đang hoạt động.");
      }

      resolved.Add(new AiTryOnAccessoryImageDto(
        product.Slug,
        await ReadAssetBytesAsync(aiAsset.FileUrl, cancellationToken),
        aiAsset.MimeType));
    }

    return resolved;
  }

  private async Task<byte[]> ReadAssetBytesAsync(string fileUrl, CancellationToken cancellationToken)
  {
    if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var absoluteUri))
    {
      if (absoluteUri.IsFile)
      {
        var filePath = absoluteUri.LocalPath;
        if (!File.Exists(filePath))
        {
          throw new FileNotFoundException("Không tìm thấy AI asset của sản phẩm đã chọn.", filePath);
        }

        return await File.ReadAllBytesAsync(filePath, cancellationToken);
      }

      using var httpClient = httpClientFactory.CreateClient();
      using var response = await httpClient.GetAsync(absoluteUri, cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    if (uploadStoragePathResolver.TryGetAbsolutePathForRequestPath(fileUrl.Trim(), out var localPath))
    {
      if (!File.Exists(localPath))
      {
        throw new FileNotFoundException("Không tìm thấy AI asset của sản phẩm đã chọn.", localPath);
      }

      return await File.ReadAllBytesAsync(localPath, cancellationToken);
    }

    throw new FileNotFoundException("AI asset phải là đường dẫn tuyệt đối hoặc /upload/... hợp lệ.", fileUrl);
  }

  private static ProductAiAsset? SelectAiAsset(Product product, string assetKind, long? variantId = null)
  {
    var candidateKinds = assetKind switch
    {
      GarmentAssetKind => new[] { CuratedGarmentAssetKind, GarmentAssetKind },
      AccessoryAssetKind => new[] { CuratedAccessoryAssetKind, AccessoryAssetKind },
      _ => new[] { assetKind }
    };

    var matchingAssets = product.AiAssets
      .Where(asset => asset.IsActive && candidateKinds.Contains(asset.AssetKind));

    if (variantId.HasValue)
    {
      var variantMatch = matchingAssets
        .OrderBy(asset => Array.IndexOf(candidateKinds, asset.AssetKind))
        .ThenBy(asset => asset.Id)
        .FirstOrDefault(asset => asset.VariantId == variantId.Value);
      if (variantMatch is not null)
      {
        return variantMatch;
      }
    }

    return matchingAssets
      .OrderBy(asset => Array.IndexOf(candidateKinds, asset.AssetKind))
      .ThenByDescending(asset => asset.VariantId.HasValue)
      .ThenBy(asset => asset.Id)
      .FirstOrDefault();
  }

  private sealed record ResolvedTryOnImage(
    string GarmentId,
    byte[] ImageBytes,
    string MimeType);
}
