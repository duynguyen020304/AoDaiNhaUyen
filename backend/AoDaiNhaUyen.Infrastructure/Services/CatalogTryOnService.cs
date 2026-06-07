using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class CatalogTryOnService(
  AppDbContext dbContext,
  IAiTryOnService aiTryOnService,
  ICachedImageValidationService imageValidationService,
  IHttpClientFactory httpClientFactory,
  IUploadStoragePathResolver uploadStoragePathResolver,
  IStorageService storageService,
  IImageVisibilityService imageVisibilityService) : ICatalogTryOnService
{
  private const string CuratedGarmentAssetKind = "tryon_garment_curated";
  private const string GarmentAssetKind = "tryon_garment";
  private const string CuratedAccessoryAssetKind = "tryon_accessory_curated";
  private const string AccessoryAssetKind = "tryon_accessory";

  public async Task<AiTryOnCatalogDto> GetCatalogAsync(
    AiTryOnCatalogQueryDto query,
    CancellationToken cancellationToken = default)
  {
    var pageSize = Math.Clamp(query.PageSize, 1, 24);
    var garmentPage = Math.Max(query.GarmentPage, 1);
    var accessoryPage = Math.Max(query.AccessoryPage, 1);
    var garmentCategory = NormalizeCategory(query.GarmentCategory);
    var accessoryCategory = NormalizeCategory(query.AccessoryCategory);

    var products = await dbContext.Products
      .AsNoTracking()
      .Include(product => product.Category)
      .Include(product => product.Variants)
      .Include(product => product.Images)
      .Include(product => product.AiAssets)
      .Where(product => product.Status == "active" && product.IsPublic)
      .Where(product => product.AiAssets.Any(asset => asset.IsActive))
      .OrderByDescending(product => product.IsFeatured)
      .ThenBy(product => product.Name)
      .ToListAsync(cancellationToken);

    var allGarments = await ResolveCatalogImageUrlsAsync(
      products
        .Where(product => product.ProductType == "ao_dai")
        .Select(product => MapCatalogItem(product, GarmentAssetKind))
        .Where(item => item is not null)
        .Cast<AiTryOnCatalogItemDto>()
        .ToList(), cancellationToken);

    var allAccessories = await ResolveCatalogImageUrlsAsync(
      products
        .Where(product => product.ProductType == "phu_kien")
        .Select(product => MapCatalogItem(product, AccessoryAssetKind))
        .Where(item => item is not null)
        .Cast<AiTryOnCatalogItemDto>()
        .ToList(), cancellationToken);

    var filteredGarments = FilterCatalogItems(allGarments, garmentCategory, supportsFeatured: true);
    var filteredAccessories = FilterCatalogItems(allAccessories, accessoryCategory, supportsFeatured: false);

    return new AiTryOnCatalogDto(
      CreatePage(filteredGarments, garmentPage, pageSize),
      CreatePage(filteredAccessories, accessoryPage, pageSize),
      CreateCategories(allGarments, includeFeatured: true),
      CreateCategories(allAccessories, includeFeatured: false));
  }

  public async Task<AiTryOnResultDto> CreateAsync(
    CatalogAiTryOnRequestDto request,
    CancellationToken cancellationToken = default)
  {
    var validation = await imageValidationService.ValidatePersonImageAsync(
      request.PersonImageBytes,
      request.PersonImageMimeType,
      null,
      cancellationToken);

    if (!validation.IsValid)
    {
      throw new InvalidOperationException(validation.Reason);
    }

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

    var aiResult = await aiTryOnService.GenerateAsync(aiRequest, cancellationToken);

    if (!string.IsNullOrEmpty(aiResult.ResultImageUrl) && aiResult.ResultImageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
    {
      var (mimeType, bytes) = DecodeDataUrl(aiResult.ResultImageUrl);
      var extension = mimeType switch
      {
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        _ => ".png"
      };
      var fileName = $"tryon-{Guid.NewGuid():N}{extension}";
      using var stream = new MemoryStream(bytes);
      var uploadResult = await storageService.UploadAsync(stream, fileName, mimeType, "private/ai-tryon", cancellationToken);

      var presignedUrl = await storageService.GeneratePresignedGetUrlAsync(uploadResult.ObjectKey, 3600, cancellationToken);

      // Only persist a UserGeneratedImage row when caller provides user context.
      // Chat callers leave UserId/GuestKeyHash null and persist their own row
      // (with correct user tracking) via SaveGeneratedTryOnAttachmentAsync.
      // Skipping here avoids double-save for the same try-on result.
      if (request.UserId is not null || !string.IsNullOrWhiteSpace(request.GuestKeyHash))
      {
        dbContext.UserGeneratedImages.Add(new UserGeneratedImage
        {
          UserId = request.UserId,
          GuestKeyHash = request.GuestKeyHash,
          ObjectKey = uploadResult.ObjectKey,
          Url = uploadResult.Url,
          Kind = "tryon_result",
          MimeType = mimeType,
          OriginalFileName = fileName,
          FileSizeBytes = bytes.LongLength,
          SourceType = "ai_tryon"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
      }

      return new AiTryOnResultDto(presignedUrl, mimeType);
    }

    return aiResult;
  }

  private async Task<IReadOnlyList<AiTryOnCatalogItemDto>> ResolveCatalogImageUrlsAsync(
    List<AiTryOnCatalogItemDto> items, CancellationToken cancellationToken)
  {
    if (items.Count == 0) return items;

    var resolved = new List<AiTryOnCatalogItemDto>(items.Count);
    foreach (var item in items)
    {
      if (item.ThumbnailUrl is null
          || item.ThumbnailUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
          || item.ThumbnailUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
          || item.ThumbnailUrl.StartsWith("/upload/", StringComparison.OrdinalIgnoreCase))
      {
        resolved.Add(item);
        continue;
      }

      var resolvedUrl = await imageVisibilityService.ResolveUrlAsync(item.ThumbnailUrl, item.IsPublic, item.PublicObjectKey, cancellationToken);
      resolved.Add(item with { ThumbnailUrl = resolvedUrl });
    }

    return resolved;
  }

  private static AiTryOnCatalogItemDto? MapCatalogItem(Product product, string assetKind)
  {
    var aiAsset = SelectAiAsset(product, assetKind);
    if (aiAsset is null)
    {
      return null;
    }

    var selectedImage = product.Images
      .OrderBy(image => image.SortOrder)
      .FirstOrDefault(image => image.IsPrimary)
      ?? product.Images.OrderBy(image => image.SortOrder).FirstOrDefault();

    var thumbnailUrl = selectedImage?.ImageUrl ?? aiAsset.FileUrl;
    var isPublic = selectedImage?.IsPublic ?? false;
    var publicObjectKey = selectedImage?.PublicObjectKey;

    var defaultVariantId = product.Variants
      .OrderByDescending(variant => variant.IsDefault)
      .ThenBy(variant => variant.Id)
      .Select(variant => (Guid?)variant.Id)
      .FirstOrDefault();

    return new AiTryOnCatalogItemDto(
      product.Id,
      defaultVariantId,
      product.Name,
      product.ProductType,
      product.Category.Slug,
      thumbnailUrl,
      aiAsset.FileUrl,
      product.IsFeatured,
      isPublic,
      publicObjectKey);
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
          item => item.Id == request.GarmentProductId.Value && item.Status == "active" && item.IsPublic,
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
    IReadOnlyList<Guid> accessoryProductIds,
    CancellationToken cancellationToken)
  {
    if (accessoryProductIds.Count == 0)
    {
      return [];
    }

    var products = await dbContext.Products
      .AsNoTracking()
      .Include(product => product.AiAssets)
      .Include(product => product.Category)
      .Where(product => accessoryProductIds.Contains(product.Id) && product.Status == "active" && product.IsPublic)
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
        product.Name,
        await ReadAssetBytesAsync(aiAsset.FileUrl, cancellationToken),
        aiAsset.MimeType,
        product.Category.Slug));
    }

    return resolved;
  }

  private async Task<byte[]> ReadAssetBytesAsync(string fileUrl, CancellationToken cancellationToken)
  {
    var normalizedFileUrl = fileUrl.Trim();

    if (uploadStoragePathResolver.TryGetAbsolutePathForRequestPath(normalizedFileUrl, out var localPath))
    {
      return await ReadLocalFileBytesAsync(localPath, cancellationToken);
    }

    if (normalizedFileUrl.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
    {
      return await ReadLocalFileBytesAsync(new Uri(normalizedFileUrl, UriKind.Absolute).LocalPath, cancellationToken);
    }

    if (Path.IsPathRooted(normalizedFileUrl))
    {
      return await ReadLocalFileBytesAsync(normalizedFileUrl, cancellationToken);
    }

    if (Uri.TryCreate(normalizedFileUrl, UriKind.Absolute, out var absoluteUri))
    {
      if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
      {
        return await ReadLocalFileBytesAsync(absoluteUri.LocalPath, cancellationToken);
      }

      using var httpClient = httpClientFactory.CreateClient();
      using var response = await httpClient.GetAsync(absoluteUri, cancellationToken);
      response.EnsureSuccessStatusCode();
      return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    throw new FileNotFoundException("AI asset phải là đường dẫn tuyệt đối hoặc /upload/... hợp lệ.", normalizedFileUrl);
  }

  private static async Task<byte[]> ReadLocalFileBytesAsync(string filePath, CancellationToken cancellationToken)
  {
    if (!File.Exists(filePath))
    {
      throw new FileNotFoundException("Không tìm thấy AI asset của sản phẩm đã chọn.", filePath);
    }

    return await File.ReadAllBytesAsync(filePath, cancellationToken);
  }

  private static ProductAiAsset? SelectAiAsset(Product product, string assetKind, Guid? variantId = null)
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

  private static IReadOnlyList<AiTryOnCatalogItemDto> FilterCatalogItems(
    IReadOnlyList<AiTryOnCatalogItemDto> items,
    string? category,
    bool supportsFeatured)
  {
    if (string.IsNullOrWhiteSpace(category) || category == "all")
    {
      return items;
    }

    if (supportsFeatured && category == "bestseller")
    {
      return items.Where(item => item.IsFeatured).ToList();
    }

    return items.Where(item => item.CategorySlug == category).ToList();
  }

  private static AiTryOnCatalogPageDto CreatePage(
    IReadOnlyList<AiTryOnCatalogItemDto> items,
    int page,
    int pageSize)
  {
    var totalItems = items.Count;
    var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
    var normalizedPage = Math.Clamp(page, 1, totalPages);
    var pageItems = items
      .Skip((normalizedPage - 1) * pageSize)
      .Take(pageSize)
      .ToList();

    return new AiTryOnCatalogPageDto(pageItems, normalizedPage, pageSize, totalItems, totalPages);
  }

  private static IReadOnlyList<AiTryOnCatalogCategoryDto> CreateCategories(
    IReadOnlyList<AiTryOnCatalogItemDto> items,
    bool includeFeatured)
  {
    var categories = new List<AiTryOnCatalogCategoryDto>
    {
      new("all", "Tất cả")
    };

    if (includeFeatured)
    {
      categories.Add(new AiTryOnCatalogCategoryDto("bestseller", "Bestseller"));
    }

    categories.AddRange(items
      .Select(item => item.CategorySlug)
      .Distinct(StringComparer.Ordinal)
      .Select(slug => new AiTryOnCatalogCategoryDto(slug, FormatCategoryLabel(slug))));

    return categories;
  }

  private static string? NormalizeCategory(string? category)
  {
    var normalized = category?.Trim();
    return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
  }

  private static string FormatCategoryLabel(string categorySlug)
  {
    return categorySlug switch
    {
      "ao-dai-truyen-thong" => "Áo dài truyền thống",
      "ao-dai-lua-tron" => "Áo dài lụa trơn",
      "ao-dai-theu-hoa" => "Áo dài thêu hoa",
      "ao-dai-cach-tan" => "Áo dài cách tân",
      "giay" => "Giày",
      "khan-dong" => "Khăn đóng",
      "man-doi-dau" => "Mấn đội đầu",
      "quat" => "Quạt",
      "tram-cai" => "Trâm cài",
      "trang-suc" => "Trang sức",
      "tui-sach" => "Túi sách",
      "tui-xach" => "Túi xách",
      _ => string.Join(
        " ",
        categorySlug
          .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(word => $"{char.ToUpperInvariant(word[0])}{word[1..]}"))
    };
  }

  private static (string MimeType, byte[] Bytes) DecodeDataUrl(string dataUrl)
  {
    var separator = dataUrl.IndexOf(',', StringComparison.Ordinal);
    var header = dataUrl[..separator];
    var mimeType = header.Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Replace("data:", string.Empty, StringComparison.OrdinalIgnoreCase);
    return (mimeType, Convert.FromBase64String(dataUrl[(separator + 1)..]));
  }

  private sealed record ResolvedTryOnImage(
    string GarmentId,
    byte[] ImageBytes,
    string MimeType);
}
