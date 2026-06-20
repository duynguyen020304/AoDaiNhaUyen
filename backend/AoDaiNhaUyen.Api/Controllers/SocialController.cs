using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/admin/social")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class SocialController(
  ISocialService socialService,
  IStorageService storageService) : ControllerBase
{
  [HttpGet("accounts")]
  public async Task<IActionResult> GetAccounts(
    [FromQuery] string? platform,
    [FromQuery] bool sync = false,
    [FromQuery] string? profileId = null,
    CancellationToken cancellationToken = default)
  {
    var accounts = await socialService.GetAccountsAsync(platform, sync, profileId, cancellationToken);
    return Ok(ApiResponseFactory.Success(accounts));
  }

  [HttpGet("connect-url")]
  public async Task<IActionResult> GetConnectUrl(
    [FromQuery] string platform,
    [FromQuery] string profileId,
    [FromQuery] string redirectUrl,
    [FromQuery] bool headless = false,
    CancellationToken cancellationToken = default)
  {
    var result = await socialService.GetConnectUrlAsync(
      new CreateSocialConnectUrlRequest(platform, profileId, redirectUrl, headless),
      cancellationToken);
    return Ok(ApiResponseFactory.Success(result));
  }

  [HttpPost("facebook/pages/select")]
  public async Task<IActionResult> SelectFacebookPage([FromBody] SelectFacebookPageRequest request, CancellationToken cancellationToken)
  {
    var accounts = await socialService.SelectFacebookPageAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success(accounts, "Đã kết nối fanpage Facebook qua Zernio."));
  }

  [HttpDelete("accounts/{id:guid}")]
  public async Task<IActionResult> DisconnectAccount(Guid id, CancellationToken cancellationToken)
  {
    await socialService.DisconnectAccountAsync(id, cancellationToken);
    return NoContent();
  }

  [HttpPost("posts")]
  public async Task<IActionResult> CreatePost([FromBody] CreateSocialPostRequest request, CancellationToken cancellationToken)
  {
    var post = await socialService.CreatePostAsync(request, cancellationToken);
    return Created($"/api/admin/social/posts/{post.Id}", ApiResponseFactory.Success(post, "Đã gửi bài viết sang Zernio."));
  }

  [HttpGet("posts")]
  public async Task<IActionResult> GetPosts(
    [FromQuery] string? platform,
    [FromQuery] Guid? accountId,
    [FromQuery] string? profileId,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var posts = await socialService.GetPostsAsync(platform, accountId, profileId, page, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(posts));
  }

  [HttpGet("analytics")]
  public async Task<IActionResult> GetAnalytics(
    [FromQuery] string platform = "facebook",
    [FromQuery] DateOnly? fromDate = null,
    [FromQuery] DateOnly? toDate = null,
    CancellationToken cancellationToken = default)
  {
    var end = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
    var start = fromDate ?? end.AddDays(-29);
    var analytics = await socialService.GetAnalyticsAsync(platform, start, end, cancellationToken);
    return Ok(ApiResponseFactory.Success(analytics));
  }

  [HttpPost("media/upload")]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> UploadMedia([FromForm] IFormFile file, CancellationToken cancellationToken = default)
  {
    var validation = ValidateSocialMedia(file);
    if (validation is not null) return validation;

    await using var stream = file.OpenReadStream();
    var upload = await storageService.UploadAsync(stream, file.FileName, file.ContentType, "public/social", cancellationToken);
    var result = new SocialMediaUploadDto(
      upload.Url,
      upload.ObjectKey,
      upload.OriginalFileName,
      upload.MimeType,
      upload.FileSize);

    return Ok(ApiResponseFactory.Success(result, "Tải media lên thành công."));
  }

  private static IActionResult? ValidateSocialMedia(IFormFile? file)
  {
    if (file is null || file.Length == 0 || string.IsNullOrWhiteSpace(file.FileName) || string.IsNullOrWhiteSpace(file.ContentType))
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("File không hợp lệ", "bad_request", "Vui lòng chọn file hợp lệ."));
    }

    var contentType = file.ContentType.Trim().ToLowerInvariant();
    var maxBytes = contentType.StartsWith("video/", StringComparison.Ordinal)
      ? 200L * 1024 * 1024
      : 10L * 1024 * 1024;
    var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "image/jpeg",
      "image/png",
      "image/webp",
      "image/gif",
      "video/mp4",
      "video/quicktime",
      "video/webm"
    };

    if (!allowedContentTypes.Contains(contentType))
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("Định dạng file không hỗ trợ", "unsupported_media_type", "Chỉ hỗ trợ JPG, PNG, WEBP, GIF, MP4, MOV hoặc WEBM."));
    }

    if (file.Length > maxBytes)
    {
      return new BadRequestObjectResult(ApiResponseFactory.Failure("File quá lớn", "file_too_large", contentType.StartsWith("video/", StringComparison.Ordinal) ? "Video tối đa 200MB." : "Ảnh tối đa 10MB."));
    }

    return null;
  }
}
