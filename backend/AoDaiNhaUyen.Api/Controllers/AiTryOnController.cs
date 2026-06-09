using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/ai-tryon")]
public sealed class AiTryOnController(
  ICatalogTryOnService catalogTryOnService,
  IImageUploadValidator imageUploadValidator,
  ILogger<AiTryOnController> logger) : ControllerBase
{
  private const long MaxImageBytes = 8 * 1024 * 1024;
  private const int MaxAccessoryImages = 3;
  private const long MaxRequestBytes = MaxImageBytes * (2 + MaxAccessoryImages);

  [HttpGet("catalog")]
  public async Task<IActionResult> GetCatalog(
    [FromQuery] int garmentPage = 1,
    [FromQuery] int accessoryPage = 1,
    [FromQuery] int pageSize = 6,
    [FromQuery] string? garmentCategory = null,
    [FromQuery] string? accessoryCategory = null,
    CancellationToken cancellationToken = default)
  {
    var result = await catalogTryOnService.GetCatalogAsync(
      new AiTryOnCatalogQueryDto(
        garmentPage,
        accessoryPage,
        pageSize,
        garmentCategory,
        accessoryCategory),
      cancellationToken);

    return Ok(ApiResponseFactory.Success(result));
  }

  [EnableRateLimiting("ai")]
  [HttpPost]
  [RequestSizeLimit(MaxRequestBytes)]
  public async Task<IActionResult> Create(
    [FromForm] IFormFile? personImage,
    [FromForm] IFormFile? garmentImage,
    [FromForm] string? garmentId,
    [FromForm] Guid? garmentProductId,
    [FromForm] Guid? garmentVariantId,
    [FromForm] List<IFormFile>? accessoryImages,
    [FromForm] List<string>? accessoryIds,
    [FromForm] List<Guid>? accessoryProductIds,
    CancellationToken cancellationToken)
  {
    var validationError = await ValidateAsync(
      personImage,
      garmentImage,
      garmentId,
      garmentProductId,
      accessoryImages,
      accessoryIds,
      cancellationToken);
    if (validationError is not null)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Dữ liệu thử đồ không hợp lệ",
        validationError.Value.Code,
        validationError.Value.Message));
    }

    try
    {
      var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
      Guid? userId = Guid.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : null;
      string? guestKeyHash = userId is null ? ComputeGuestKeyHash(HttpContext) : null;

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
          await ReadAccessoryImagesAsync(accessoryImages ?? [], accessoryIds ?? [], cancellationToken),
          userId,
          guestKeyHash),
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
    catch (ImageValidationConfigurationException ex)
    {
      var errorId = LogSafeError(ex, "Image validation configuration failed");
      return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponseFactory.Failure(
        "Dịch vụ kiểm tra ảnh chưa được cấu hình",
        "image_validation_not_configured",
        $"Dịch vụ hiện không khả dụng. Mã lỗi: {errorId}"));
    }
    catch (ImageValidationProviderException)
    {
      return StatusCode(StatusCodes.Status502BadGateway, ApiResponseFactory.Failure(
        "Không thể kiểm tra ảnh thử đồ",
        "image_validation_failed",
        "Không thể kiểm tra ảnh thử đồ lúc này. Vui lòng thử lại sau."));
    }
    catch (AiTryOnConfigurationException ex)
    {
      var errorId = LogSafeError(ex, "AI try-on configuration failed");
      return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponseFactory.Failure(
        "Dịch vụ thử đồ AI chưa được cấu hình",
        "vertex_ai_not_configured",
        $"Dịch vụ hiện không khả dụng. Mã lỗi: {errorId}"));
    }
    catch (AiTryOnProviderException ex)
    {
      var errorId = LogSafeError(ex, "AI try-on provider failed");
      return StatusCode(StatusCodes.Status502BadGateway, ApiResponseFactory.Failure(
        "Không thể tạo ảnh thử đồ",
        "vertex_ai_failed",
        $"Không thể tạo ảnh thử đồ lúc này. Mã lỗi: {errorId}"));
    }
  }

  private async Task<(string Code, string Message)?> ValidateAsync(
    IFormFile? personImage,
    IFormFile? garmentImage,
    string? garmentId,
    Guid? garmentProductId,
    IReadOnlyList<IFormFile>? accessoryImages,
    IReadOnlyList<string>? accessoryIds,
    CancellationToken cancellationToken)
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

    var personError = await ValidateImageAsync(personImage, cancellationToken);
    if (personError is not null)
    {
      return personError;
    }

    if (garmentImage is not null)
    {
      var garmentError = await ValidateImageAsync(garmentImage, cancellationToken);
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
      var accessoryError = await ValidateImageAsync(accessoryImage, cancellationToken);
      if (accessoryError is not null)
      {
        return accessoryError;
      }
    }

    return null;
  }

  private async Task<(string Code, string Message)?> ValidateImageAsync(IFormFile file, CancellationToken cancellationToken)
  {
    var bytes = await ReadFileAsync(file, cancellationToken);
    var validation = imageUploadValidator.Validate(file.ContentType, bytes, file.Length, MaxImageBytes);
    return validation.IsValid ? null : (validation.ErrorCode ?? "invalid_image", validation.ErrorMessage ?? "Ảnh không hợp lệ.");
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
        accessoryIds[i],
        await ReadFileAsync(accessoryImages[i], cancellationToken),
        accessoryImages[i].ContentType,
        "unknown"));
    }

    return results;
  }

  private string LogSafeError(Exception exception, string message)
  {
    var errorId = Guid.NewGuid().ToString("N");
    logger.LogError(exception, "{Message}. ErrorId={ErrorId}", message, errorId);
    return errorId;
  }

  private static string? ComputeGuestKeyHash(HttpContext context)
  {
    var guestKey = context.Request.Headers["X-Guest-Key"].FirstOrDefault()
      ?? context.Connection.RemoteIpAddress?.ToString();
    if (string.IsNullOrWhiteSpace(guestKey)) return null;
    var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(guestKey.Trim()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}