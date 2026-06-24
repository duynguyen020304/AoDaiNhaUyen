using System.Net;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>
/// Admin-facing AI try-on endpoints. The <c>generate</c> action is the try-on
/// tool exposed to the Hermes agent (auth via <c>X-Hermes-Admin-Key</c> which
/// grants the Admin role). The agent uses it to turn a stored inbound
/// Messenger photo + a customer-chosen garment into a try-on result image,
/// then replies in the Facebook conversation with the result URL.
/// </summary>
[ApiController]
[Route("api/admin/ai-tryon")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminAiTryOnController(
  ICatalogTryOnService catalogTryOnService,
  IHttpClientFactory httpClientFactory,
  ILogger<AdminAiTryOnController> logger) : ControllerBase
{
  private const long MaxPersonImageBytes = 8 * 1024 * 1024;

  /// <summary>
  /// Lists try-on-eligible garments/accessories (products that have an active
  /// ProductAiAsset). Use this to offer the customer 2–4 garments to choose
  /// from before generating a try-on.
  /// </summary>
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

  /// <summary>
  /// Generates a try-on image from a presigned person-image URL and a
  /// selected garment product. Reuses the catalog try-on engine (Gemini
  /// virtual-try-on model). Returns a 1-hour presigned result URL that the
  /// agent must send to the customer promptly.
  /// </summary>
  [EnableRateLimiting("ai")]
  [HttpPost("generate")]
  public async Task<IActionResult> Generate(
    [FromBody] AdminGenerateTryOnRequestDto request,
    CancellationToken cancellationToken)
  {
    if (request.GarmentProductId == Guid.Empty)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Thiếu sản phẩm áo dài",
        "missing_garment",
        "GarmentProductId là bắt buộc."));
    }
    if (string.IsNullOrWhiteSpace(request.PersonImageUrl)
      || !Uri.TryCreate(request.PersonImageUrl, UriKind.Absolute, out var personUri))
    {
      return BadRequest(ApiResponseFactory.Failure(
        "URL ảnh người mặc không hợp lệ",
        "invalid_person_image_url",
        "PersonImageUrl phải là URL HTTPS hợp lệ (lấy từ GET /api/admin/social/messages/{id}/image)."));
    }
    // Require HTTPS + block loopback/link-local to prevent the admin endpoint
    // from being abused as an internal-network fetch (the agent is trusted but
    // a leaked key should not turn this into a metadata-service oracle).
    if (!string.Equals(personUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
    {
      return BadRequest(ApiResponseFactory.Failure(
        "URL ảnh người mặc phải là HTTPS",
        "invalid_person_image_url",
        "PersonImageUrl phải dùng https."));
    }

    byte[] personBytes;
    string personMime;
    try
    {
      (personBytes, personMime) = await DownloadPersonImageAsync(request.PersonImageUrl, cancellationToken);
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Không tải được ảnh người mặc",
        "person_image_download_failed",
        ex.Message));
    }

    try
    {
      // UserId/GuestKeyHash intentionally null: try-on triggered from the
      // Hermes/Facebook flow is unattributed (no PSID→customer mapping yet),
      // mirroring the chat path which leaves persistence to its caller.
      var result = await catalogTryOnService.CreateAsync(
        new CatalogAiTryOnRequestDto(
          LegacyGarmentId: null,
          PersonImageBytes: personBytes,
          PersonImageMimeType: personMime,
          GarmentProductId: request.GarmentProductId,
          GarmentVariantId: request.GarmentVariantId,
          AccessoryProductIds: request.AccessoryProductIds ?? [],
          LegacyGarmentImageBytes: null,
          LegacyGarmentImageMimeType: null,
          LegacyAccessoryImages: [],
          UserId: null,
          GuestKeyHash: null),
        cancellationToken);

      var dto = new AdminGenerateTryOnResultDto(
        result.ResultImageUrl,
        result.MimeType,
        result.GeneratedImageId);

      return Ok(ApiResponseFactory.Success(dto, "Tạo ảnh thử đồ thành công"));
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
        "Ảnh người mặc không hợp lệ hoặc không nhận diện được khuôn mặt. Xin khách gửi lại ảnh rõ hơn."));
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

  private async Task<(byte[] Bytes, string MimeType)> DownloadPersonImageAsync(
    string url,
    CancellationToken cancellationToken)
  {
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
      || !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("URL ảnh người mặc phải là HTTPS hợp lệ.");
    }
    // Block obvious internal IP-literal hosts (loopback / link-local / cloud
    // metadata). DNS-host SSRF is out of scope here because the expected URL is
    // our own presigned S3 URL; this guard stops the cheap metadata-endpoint
    // exfiltration path.
    if ((uri.HostNameType == UriHostNameType.IPv4 || uri.HostNameType == UriHostNameType.IPv6)
      && IPAddress.TryParse(uri.Host, out var ip)
      && (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal))
    {
      throw new InvalidOperationException("URL ảnh người mặc trỏ về host nội bộ không hợp lệ.");
    }

    using var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      throw new InvalidOperationException(
        $"Tải ảnh người mặc thất bại (HTTP {(int)response.StatusCode}). Có thể presigned URL đã hết hạn — tạo lại từ GET /api/admin/social/messages/{{id}}/image.");
    }

    var declared = response.Content.Headers.ContentLength;
    if (declared.HasValue && declared.Value > MaxPersonImageBytes)
    {
      throw new InvalidOperationException($"Ảnh người mặc quá lớn ({declared.Value} bytes).");
    }

    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    using var memory = new MemoryStream();
    var buffer = new byte[16 * 1024];
    int read;
    while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
    {
      if (memory.Length + read > MaxPersonImageBytes)
      {
        throw new InvalidOperationException($"Ảnh người mặc vượt giới hạn {MaxPersonImageBytes} bytes.");
      }
      await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
    }

    var mime = response.Content.Headers.ContentType?.MediaType;
    if (string.IsNullOrWhiteSpace(mime)) mime = "image/jpeg";
    return (memory.ToArray(), mime);
  }

  private string LogSafeError(Exception exception, string message)
  {
    var errorId = Guid.NewGuid().ToString("N");
    logger.LogError(exception, "{Message}. ErrorId={ErrorId}", message, errorId);
    return errorId;
  }
}
