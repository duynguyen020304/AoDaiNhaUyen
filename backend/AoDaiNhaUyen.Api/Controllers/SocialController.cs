using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Social;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/admin/social")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class SocialController(
  ISocialService socialService,
  IStorageService storageService,
  IOptions<ZernioSettings> zernioOptions) : ControllerBase
{
  private readonly ZernioSettings zernioSettings = zernioOptions.Value;

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

  [HttpGet("posts/{postId}")]
  public async Task<IActionResult> GetPost(string postId, CancellationToken cancellationToken)
  {
    var post = await socialService.GetPostAsync(postId, cancellationToken);
    return Ok(ApiResponseFactory.Success(post));
  }

  [HttpPut("posts/{postId}")]
  public async Task<IActionResult> UpdatePost(
    string postId,
    [FromBody] UpdateSocialPostRequest request,
    CancellationToken cancellationToken)
  {
    var post = await socialService.UpdatePostAsync(postId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(post, "Đã cập nhật bài viết Zernio."));
  }

  [HttpDelete("posts/{postId}")]
  public async Task<IActionResult> DeletePost(string postId, CancellationToken cancellationToken)
  {
    await socialService.DeletePostAsync(postId, cancellationToken);
    return Ok(ApiResponseFactory.Success(new { deleted = true }, "Đã xóa bài viết Zernio."));
  }

  [HttpPost("posts/{postId}/unpublish")]
  public async Task<IActionResult> UnpublishPost(
    string postId,
    [FromBody] UnpublishSocialPostRequest request,
    CancellationToken cancellationToken)
  {
    var result = await socialService.UnpublishPostAsync(postId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã gỡ bài viết khỏi nền tảng."));
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

  [HttpGet("comments")]
  public async Task<IActionResult> GetCommentedPosts(
    [FromQuery] string? platform = "facebook",
    [FromQuery] string? accountId = null,
    [FromQuery] string? profileId = null,
    [FromQuery] string? cursor = null,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var posts = await socialService.GetCommentedPostsAsync(platform, accountId, profileId, cursor, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(posts));
  }

  [HttpGet("comments/{postId}")]
  public async Task<IActionResult> GetComments(
    string postId,
    [FromQuery] string accountId,
    [FromQuery] string? cursor = null,
    [FromQuery] int limit = 50,
    CancellationToken cancellationToken = default)
  {
    var comments = await socialService.GetCommentsAsync(postId, accountId, cursor, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(comments));
  }

  [HttpPost("comments/{postId}")]
  public async Task<IActionResult> ReplyToComment(string postId, [FromBody] CreateSocialCommentReplyRequest request, CancellationToken cancellationToken)
  {
    var result = await socialService.ReplyToCommentAsync(postId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã gửi bình luận qua Zernio."));
  }

  [HttpDelete("comments/{postId}/{commentId}")]
  public async Task<IActionResult> DeleteComment(
    string postId,
    string commentId,
    [FromQuery] string accountId,
    CancellationToken cancellationToken)
  {
    var result = await socialService.DeleteCommentAsync(postId, accountId, commentId, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã xóa bình luận qua Zernio."));
  }

  [HttpPatch("comments/{postId}/{commentId}/visibility")]
  public async Task<IActionResult> ToggleCommentHidden(
    string postId,
    string commentId,
    [FromQuery] string accountId,
    [FromBody] ToggleSocialCommentHiddenRequest request,
    CancellationToken cancellationToken)
  {
    var result = await socialService.ToggleCommentHiddenAsync(postId, accountId, commentId, request.IsHidden, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, request.IsHidden ? "Đã ẩn bình luận qua Zernio." : "Đã hiện bình luận qua Zernio."));
  }

  [HttpGet("conversations")]
  public async Task<IActionResult> GetConversations(
    [FromQuery] string? platform = "facebook",
    [FromQuery] string? accountId = null,
    [FromQuery] string? profileId = null,
    [FromQuery] string? cursor = null,
    [FromQuery] int limit = 25,
    CancellationToken cancellationToken = default)
  {
    var conversations = await socialService.GetConversationsAsync(platform, accountId, profileId, cursor, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(conversations));
  }

  [HttpGet("conversations/{conversationId}/messages")]
  public async Task<IActionResult> GetConversationMessages(
    string conversationId,
    [FromQuery] string accountId,
    [FromQuery] string? cursor = null,
    [FromQuery] int limit = 50,
    CancellationToken cancellationToken = default)
  {
    var messages = await socialService.GetConversationMessagesAsync(conversationId, accountId, cursor, limit, cancellationToken);
    return Ok(ApiResponseFactory.Success(messages));
  }

  [HttpPost("conversations/{conversationId}/messages")]
  public async Task<IActionResult> SendMessage(string conversationId, [FromBody] SendSocialMessageRequest request, CancellationToken cancellationToken)
  {
    var result = await socialService.SendMessageAsync(conversationId, request, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã gửi tin nhắn qua Zernio."));
  }

  [HttpPost("conversations/{conversationId}/read")]
  public async Task<IActionResult> MarkConversationRead(
    string conversationId,
    [FromBody] MarkSocialConversationReadRequest request,
    CancellationToken cancellationToken)
  {
    var result = await socialService.MarkConversationReadAsync(conversationId, request.AccountId, cancellationToken);
    return Ok(ApiResponseFactory.Success(result, "Đã đánh dấu đã đọc qua Zernio."));
  }

  [HttpPost("~/api/webhooks/zernio")]
  [AllowAnonymous]
  public async Task<IActionResult> IngestZernioWebhook(CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(zernioSettings.WebhookSecret))
    {
      return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponseFactory.Failure(
        "Webhook chưa được cấu hình",
        "webhook_secret_missing",
        "Vui lòng cấu hình Zernio webhook secret trước khi nhận sự kiện."));
    }

    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
    var rawBody = await reader.ReadToEndAsync(cancellationToken);
    if (!VerifyZernioWebhookSignature(rawBody, zernioSettings.WebhookSecret, Request.Headers))
    {
      return Unauthorized(ApiResponseFactory.Failure(
        "Chữ ký webhook không hợp lệ",
        "invalid_webhook_signature",
        "Không thể xác thực webhook Zernio."));
    }

    JsonElement payload;
    try
    {
      using var document = JsonDocument.Parse(rawBody);
      payload = document.RootElement.Clone();
    }
    catch (JsonException)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Payload webhook không hợp lệ",
        "invalid_webhook_payload",
        "Nội dung webhook không phải JSON hợp lệ."));
    }

    await socialService.IngestZernioWebhookAsync(payload, cancellationToken);
    return Ok(ApiResponseFactory.Success(new { ingested = true }, "Đã nhận webhook Zernio."));
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

  private static bool VerifyZernioWebhookSignature(string rawBody, string secret, IHeaderDictionary headers)
  {
    var provided = GetWebhookSignature(headers);
    if (string.IsNullOrWhiteSpace(provided)) return false;

    provided = provided.Trim();
    if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
    {
      provided = provided[7..];
    }

    var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
    var secretBytes = Encoding.UTF8.GetBytes(secret);
    using var hmac = new HMACSHA256(secretBytes);
    var signatureBytes = hmac.ComputeHash(bodyBytes);
    var expectedHex = Convert.ToHexString(signatureBytes).ToLowerInvariant();
    var expectedBase64 = Convert.ToBase64String(signatureBytes);

    return FixedTimeEquals(provided, expectedHex) || FixedTimeEquals(provided, expectedBase64);
  }

  private static string? GetWebhookSignature(IHeaderDictionary headers)
  {
    foreach (var headerName in new[] { "X-Zernio-Signature", "X-Hub-Signature-256", "X-Signature" })
    {
      if (headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value.FirstOrDefault()))
      {
        return value.FirstOrDefault();
      }
    }

    return null;
  }

  private static bool FixedTimeEquals(string provided, string expected)
  {
    var providedBytes = Encoding.UTF8.GetBytes(provided);
    var expectedBytes = Encoding.UTF8.GetBytes(expected);
    return providedBytes.Length == expectedBytes.Length &&
      CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
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
