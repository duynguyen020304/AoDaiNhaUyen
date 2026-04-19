using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/ai-tryon")]
public sealed class AiTryOnController(ICatalogTryOnService catalogTryOnService) : ControllerBase
{
  private const long MaxImageBytes = 8 * 1024 * 1024;
  private const int MaxAccessoryImages = 3;
  private const long MaxRequestBytes = MaxImageBytes * (2 + MaxAccessoryImages);

  [HttpGet("catalog")]
  public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
  {
    var result = await catalogTryOnService.GetCatalogAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(result));
  }

  [HttpPost]
  [RequestSizeLimit(MaxRequestBytes)]
  public async Task<IActionResult> Create(
    [FromForm] IFormFile? personImage,
    [FromForm] IFormFile? garmentImage,
    [FromForm] string? garmentId,
    [FromForm] long? garmentProductId,
    [FromForm] long? garmentVariantId,
    [FromForm] List<IFormFile>? accessoryImages,
    [FromForm] List<string>? accessoryIds,
    [FromForm] List<long>? accessoryProductIds,
    CancellationToken cancellationToken)
  {
    var validationError = Validate(
      personImage,
      garmentImage,
      garmentId,
      garmentProductId,
      accessoryImages,
      accessoryIds);
    if (validationError is not null)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Dữ liệu thử đồ không hợp lệ",
        validationError.Value.Code,
        validationError.Value.Message));
    }

    try
    {
      var result = await catalogTryOnService.CreateAsync(
        new CatalogAiTryOnRequestDto(
          garmentId?.Trim(),
          await ReadFileAsync(personImage!, cancellationToken),
          personImage!.ContentType,
          garmentProductId,
          garmentVariantId,
          accessoryProductIds ?? [],
          garmentImage is null ? null : await ReadFileAsync(garmentImage, cancellationToken),
          garmentImage?.ContentType,
          await ReadAccessoryImagesAsync(accessoryImages ?? [], accessoryIds ?? [], cancellationToken)),
        cancellationToken);

      return Ok(ApiResponseFactory.Success(result, "Tạo ảnh thử đồ thành công"));
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Dữ liệu thử đồ không hợp lệ",
        "invalid_tryon_selection",
        ex.Message));
    }
    catch (FileNotFoundException ex)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Không thể tải AI asset của sản phẩm đã chọn",
        "missing_tryon_asset",
        ex.Message));
    }
    catch (AiTryOnConfigurationException ex)
    {
      return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponseFactory.Failure(
        "Dịch vụ thử đồ AI chưa được cấu hình",
        "vertex_ai_not_configured",
        ex.Message));
    }
    catch (AiTryOnProviderException ex)
    {
      return StatusCode(StatusCodes.Status502BadGateway, ApiResponseFactory.Failure(
        "Không thể tạo ảnh thử đồ",
        "vertex_ai_failed",
        ex.Message));
    }
  }

  private static (string Code, string Message)? Validate(
    IFormFile? personImage,
    IFormFile? garmentImage,
    string? garmentId,
    long? garmentProductId,
    IReadOnlyList<IFormFile>? accessoryImages,
    IReadOnlyList<string>? accessoryIds)
  {
    if (personImage is null)
    {
      return ("invalid_image", "Person image is required.");
    }

    if (garmentImage is null && !garmentProductId.HasValue)
    {
      return ("invalid_image", "Garment image or garment product selection is required.");
    }

    if (garmentImage is not null && string.IsNullOrWhiteSpace(garmentId) && !garmentProductId.HasValue)
    {
      return ("missing_garment", "Garment selection is required.");
    }

    var personError = ValidateImage(personImage, "Person image");
    if (personError is not null)
    {
      return personError;
    }

    if (garmentImage is not null)
    {
      var garmentError = ValidateImage(garmentImage, "Garment image");
      if (garmentError is not null)
      {
        return garmentError;
      }
    }

    if ((accessoryImages?.Count ?? 0) > MaxAccessoryImages)
    {
      return ("invalid_image", $"At most {MaxAccessoryImages} accessory images are allowed.");
    }

    if ((accessoryImages?.Count ?? 0) != (accessoryIds?.Count ?? 0))
    {
      return ("invalid_image", "Accessory image count must match accessory id count.");
    }

    foreach (var accessoryImage in accessoryImages ?? [])
    {
      var accessoryError = ValidateImage(accessoryImage, "Accessory image");
      if (accessoryError is not null)
      {
        return accessoryError;
      }
    }

    return null;
  }

  private static (string Code, string Message)? ValidateImage(IFormFile file, string label)
  {
    if (file.Length <= 0)
    {
      return ("invalid_image", $"{label} is empty.");
    }

    if (file.Length > MaxImageBytes)
    {
      return ("invalid_image", $"{label} must be 8MB or smaller.");
    }

    if (string.IsNullOrWhiteSpace(file.ContentType)
        || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || file.ContentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase))
    {
      return ("invalid_image", $"{label} must be an image (GIF is not supported).");
    }

    return null;
  }

  private static async Task<byte[]> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
  {
    await using var stream = file.OpenReadStream();
    using var memoryStream = new MemoryStream();
    await stream.CopyToAsync(memoryStream, cancellationToken);
    return memoryStream.ToArray();
  }

  private static async Task<IReadOnlyList<AiTryOnAccessoryImageDto>> ReadAccessoryImagesAsync(
    IReadOnlyList<IFormFile> accessoryImages,
    IReadOnlyList<string> accessoryIds,
    CancellationToken cancellationToken)
  {
    var results = new List<AiTryOnAccessoryImageDto>(accessoryImages.Count);

    for (var i = 0; i < accessoryImages.Count; i++)
    {
      results.Add(new AiTryOnAccessoryImageDto(
        accessoryIds[i],
        await ReadFileAsync(accessoryImages[i], cancellationToken),
        accessoryImages[i].ContentType));
    }

    return results;
  }
}
