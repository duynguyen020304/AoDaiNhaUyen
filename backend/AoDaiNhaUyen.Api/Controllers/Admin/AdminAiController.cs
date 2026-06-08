using System.Text.Json;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>AI-powered admin assistant endpoints.</summary>
[ApiController]
[Route("api/admin/ai")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminAiController(
  IAdminAgentService agentService,
  ILogger<AdminAiController> logger) : ControllerBase
{
  /// <summary>Stream an AI chat conversation with tool-calling via SSE.</summary>
  [HttpPost("chat")]
  public async Task StreamChat(
    AdminAiChatRequest request,
    CancellationToken cancellationToken)
  {
    var adminUserId = GetAdminUserId();
    if (adminUserId is null)
    {
      Response.StatusCode = 401;
      return;
    }

    Response.ContentType = "text/event-stream";
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("Connection", "keep-alive");
    Response.Headers.Append("X-Accel-Buffering", "no");

    var validationError = ValidateChatRequest(request);
    if (validationError is not null)
    {
      var errorChunk = new { type = "error", content = validationError };
      await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorChunk)}\n\n", cancellationToken);
      await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
      return;
    }

    try
    {
      await foreach (var chunk in agentService.StreamChatAsync(request, adminUserId.Value, cancellationToken))
      {
        var json = JsonSerializer.Serialize(chunk);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // Client disconnected — normal
    }
    catch (Exception ex)
    {
      var traceId = HttpContext.TraceIdentifier;
      logger.LogError(ex, "[AdminAI] StreamChat failed. TraceId={TraceId}", traceId);
      var errorChunk = new { type = "error", content = $"Lỗi hệ thống. Mã tra cứu: {traceId}" };
      await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorChunk)}\n\n", cancellationToken);
    }

    await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
  }

  /// <summary>Confirm or reject a pending AI action.</summary>
  [HttpPost("action/confirm")]
  public async Task<ActionResult<ApiResponse<object>>> ConfirmAction(
    AdminAiConfirmRequest request,
    CancellationToken cancellationToken)
  {
    var adminUserId = GetAdminUserId();
    if (adminUserId is null)
      return Unauthorized(ApiResponseFactory.Failure("Không xác thực.", "unauthorized", "Vui lòng đăng nhập lại."));

    var ok = await agentService.ConfirmActionAsync(request.ActionId, request.Approved, adminUserId.Value, cancellationToken);
    return ok
      ? Ok(ApiResponseFactory.Success<object?>(null, request.Approved ? "Đã xác nhận hành động." : "Đã từ chối hành động."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy hành động chờ xác nhận.", "not_found", "Hành động không tồn tại hoặc đã được xử lý."));
  }

  /// <summary>Get proactive AI suggestions for the admin dashboard.</summary>
  [HttpGet("suggestions")]
  public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminAiSuggestionResponse>>>> GetSuggestions(
    CancellationToken cancellationToken)
  {
    var suggestions = await agentService.GetSuggestionsAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(suggestions, "Lấy gợi ý AI thành công."));
  }

  /// <summary>Toggle AI autonomy mode on/off.</summary>
  [HttpPost("auto-mode/toggle")]
  public ActionResult<ApiResponse<object>> ToggleAutoMode(
    [FromBody] ToggleAutoModeRequest request)
  {
    var adminUserId = GetAdminUserId();
    if (adminUserId is null)
      return Unauthorized(ApiResponseFactory.Failure("Không xác định được quản trị viên.", "unauthorized", "Vui lòng đăng nhập lại."));

    var store = HttpContext.RequestServices.GetRequiredService<IAutoModeStore>();
    if (request.Enabled) store.Enable(adminUserId.Value);
    else store.Disable(adminUserId.Value);

    return Ok(ApiResponseFactory.Success<object?>(null,
      request.Enabled ? "Đã bật chế độ tự động trong 30 phút." : "Đã tắt chế độ tự động."));
  }

  /// <summary>Get current auto mode status.</summary>
  [HttpGet("auto-mode/status")]
  public ActionResult<ApiResponse<object>> GetAutoModeStatus()
  {
    var adminUserId = GetAdminUserId();
    if (adminUserId is null)
      return Unauthorized(ApiResponseFactory.Failure("Không xác định được quản trị viên.", "unauthorized", "Vui lòng đăng nhập lại."));

    var store = HttpContext.RequestServices.GetRequiredService<IAutoModeStore>();
    var isAutoMode = store.IsAutoModeEnabled(adminUserId.Value);
    return Ok(ApiResponseFactory.Success<object>(
      new { isAutoMode },
      isAutoMode ? "Chế độ tự động đang bật." : "Chế độ tự động đang tắt."));
  }

  /// <summary>Get store health score.</summary>
  [HttpGet("store-health")]
  public async Task<ActionResult<ApiResponse<object>>> GetStoreHealth(
    CancellationToken cancellationToken)
  {
    var inventoryService = HttpContext.RequestServices.GetRequiredService<IAdminInventoryService>();
    var health = await inventoryService.GetStoreHealthScoreAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success<object>(health, "Lấy điểm sức khỏe cửa hàng thành công."));
  }

  private static string? ValidateChatRequest(AdminAiChatRequest request)
  {
    const int maxMessageLength = 4000;
    const int maxConversationIdLength = 64;

    var hasMessage = !string.IsNullOrWhiteSpace(request.Message);
    var hasConversationId = !string.IsNullOrWhiteSpace(request.ConversationId);

    if (!hasMessage && !hasConversationId)
      return "Tin nhắn không được để trống.";

    if (request.Message.Length > maxMessageLength)
      return $"Tin nhắn quá dài. Tối đa {maxMessageLength} ký tự.";

    if (hasConversationId)
    {
      var conversationId = request.ConversationId!.Trim();
      if (conversationId.Length > maxConversationIdLength || conversationId.Any(ch => !char.IsAsciiHexDigit(ch)))
        return "Mã cuộc trò chuyện không hợp lệ.";
    }

    return null;
  }

  private Guid? GetAdminUserId()
  {
    var sid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    return sid is not null && Guid.TryParse(sid, out var id) ? id : null;
  }
}

public sealed record ToggleAutoModeRequest(bool Enabled);
