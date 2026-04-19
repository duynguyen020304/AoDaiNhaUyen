using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/chat/threads")]
public sealed class ChatController(IStylistChatService stylistChatService) : ControllerBase
{
  private const string GuestCookieName = "stylist_guest";
  private const long MaxAttachmentBytes = 8 * 1024 * 1024;
  private const int MaxAttachments = 3;

  [HttpGet]
  public async Task<IActionResult> List(CancellationToken cancellationToken)
  {
    var threads = await stylistChatService.ListThreadsAsync(GetCurrentUserId(), GetOrCreateGuestKey(), cancellationToken);
    ClearGuestCookieIfClaimed();
    return Ok(ApiResponseFactory.Success(threads));
  }

  [HttpPost]
  public async Task<IActionResult> Create(CancellationToken cancellationToken)
  {
    var thread = await stylistChatService.CreateThreadAsync(GetCurrentUserId(), GetOrCreateGuestKey(), cancellationToken);
    ClearGuestCookieIfClaimed();
    return Ok(ApiResponseFactory.Success(thread, "Tạo cuộc trò chuyện thành công"));
  }

  [HttpGet("{threadId:long}")]
  public async Task<IActionResult> Get(long threadId, CancellationToken cancellationToken)
  {
    try
    {
      var thread = await stylistChatService.GetThreadAsync(threadId, GetCurrentUserId(), GetOrCreateGuestKey(), cancellationToken);
      ClearGuestCookieIfClaimed();
      return Ok(ApiResponseFactory.Success(thread));
    }
    catch (InvalidOperationException ex)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy cuộc trò chuyện", "thread_not_found", ex.Message));
    }
  }

  [HttpPost("{threadId:long}/messages")]
  [RequestSizeLimit(MaxAttachmentBytes * MaxAttachments)]
  public async Task<IActionResult> AddMessage(
    long threadId,
    [FromForm] string? message,
    [FromForm] string? clientMessageId,
    [FromForm] List<IFormFile>? attachments,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(message))
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu tin nhắn không hợp lệ", "missing_message", "Tin nhắn không được để trống."));
    }

    try
    {
      var thread = await stylistChatService.AddMessageAsync(
        threadId,
        GetCurrentUserId(),
        GetOrCreateGuestKey(),
        message,
        clientMessageId,
        await NormalizeAttachmentsAsync(attachments ?? [], cancellationToken),
        cancellationToken);
      ClearGuestCookieIfClaimed();
      return Ok(ApiResponseFactory.Success(thread, "Gửi tin nhắn thành công"));
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Không thể xử lý tin nhắn", "chat_failed", ex.Message));
    }
  }

  [HttpPost("{threadId:long}/try-on")]
  public async Task<IActionResult> ExecuteTryOn(
    long threadId,
    [FromBody] ExecuteTryOnRequest? request,
    CancellationToken cancellationToken)
  {
    try
    {
      var message = await stylistChatService.ExecuteTryOnAsync(
        threadId,
        GetCurrentUserId(),
        GetOrCreateGuestKey(),
        request?.GarmentProductId,
        request?.AccessoryProductIds ?? [],
        cancellationToken);
      ClearGuestCookieIfClaimed();
      return Ok(ApiResponseFactory.Success(message, "Tạo ảnh thử đồ trong chat thành công"));
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Không thể tạo ảnh thử đồ", "chat_tryon_failed", ex.Message));
    }
  }

  private async Task<IReadOnlyList<IncomingChatAttachmentDto>> NormalizeAttachmentsAsync(
    IReadOnlyList<IFormFile> attachments,
    CancellationToken cancellationToken)
  {
    if (attachments.Count > MaxAttachments)
    {
      throw new InvalidOperationException($"Chỉ được gửi tối đa {MaxAttachments} ảnh mỗi lượt.");
    }

    var results = new List<IncomingChatAttachmentDto>(attachments.Count);
    foreach (var attachment in attachments)
    {
      if (attachment.Length <= 0 || attachment.Length > MaxAttachmentBytes)
      {
        throw new InvalidOperationException("Ảnh đính kèm phải lớn hơn 0 và không quá 8MB.");
      }

      if (string.IsNullOrWhiteSpace(attachment.ContentType) ||
          !attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(attachment.ContentType, "image/gif", StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("Chỉ hỗ trợ ảnh PNG, JPG hoặc WEBP.");
      }

      await using var stream = attachment.OpenReadStream();
      using var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream, cancellationToken);
      results.Add(new IncomingChatAttachmentDto(
        "user_image",
        attachment.FileName,
        attachment.ContentType,
        memoryStream.ToArray()));
    }

    return results;
  }

  private string GetOrCreateGuestKey()
  {
    if (Request.Cookies.TryGetValue(GuestCookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
    {
      return existing;
    }

    var created = Guid.NewGuid().ToString("N");
    Response.Cookies.Append(GuestCookieName, created, BuildGuestCookieOptions());
    return created;
  }

  private void ClearGuestCookieIfClaimed()
  {
    if (GetCurrentUserId().HasValue && Request.Cookies.ContainsKey(GuestCookieName))
    {
      Response.Cookies.Delete(GuestCookieName, new CookieOptions { Path = "/" });
    }
  }

  private long? GetCurrentUserId()
  {
    var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return long.TryParse(raw, out var userId) ? userId : null;
  }

  private static CookieOptions BuildGuestCookieOptions() => new()
  {
    HttpOnly = true,
    Secure = false,
    SameSite = SameSiteMode.Lax,
    Path = "/",
    Expires = DateTimeOffset.UtcNow.AddDays(30)
  };

  public sealed record ExecuteTryOnRequest(long? GarmentProductId, IReadOnlyList<long>? AccessoryProductIds);
}
