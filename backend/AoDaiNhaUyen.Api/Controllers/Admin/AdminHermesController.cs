using System.Text.Json;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Hermes autonomous admin agent control endpoints.</summary>
[ApiController]
[Route("api/admin/hermes")]
public sealed class AdminHermesController(
  IHermesAgentService hermesAgentService,
  ILogger<AdminHermesController> logger) : ControllerBase
{
  /// <summary>Get Hermes agent status and latest heartbeat.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpGet("status")]
  public async Task<ActionResult<ApiResponse<HermesStatusResponse>>> GetStatus(CancellationToken cancellationToken)
  {
    var status = await hermesAgentService.GetStatusAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(status, "Lấy trạng thái Hermes thành công."));
  }

  /// <summary>List recent Hermes runs.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpGet("runs")]
  public async Task<ActionResult<ApiResponse<IReadOnlyList<HermesRunSummaryResponse>>>> ListRuns(CancellationToken cancellationToken)
  {
    var runs = await hermesAgentService.ListRunsAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(runs, "Lấy lịch sử Hermes thành công."));
  }

  /// <summary>Stream an admin prompt through Hermes Agent.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpPost("chat")]
  public async Task StreamChat(HermesChatRequest request, CancellationToken cancellationToken)
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
      await WriteChunkAsync(new HermesStreamChunk("error", validationError), cancellationToken);
      await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
      return;
    }

    try
    {
      await foreach (var chunk in hermesAgentService.StreamChatAsync(request, adminUserId.Value, cancellationToken))
      {
        await WriteChunkAsync(chunk, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
      }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      // Client disconnected.
    }
    catch (Exception ex)
    {
      var traceId = HttpContext.TraceIdentifier;
      logger.LogError(ex, "[HermesAdmin] StreamChat failed. TraceId={TraceId}", traceId);
      await WriteChunkAsync(new HermesStreamChunk("error", $"Lỗi Hermes Agent. Mã tra cứu: {traceId}"), cancellationToken);
    }

    await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
  }

  /// <summary>Record a Hermes runner heartbeat. Called by VPS cron/script.</summary>
  [Authorize(AuthenticationSchemes = HermesAdminAuthOptions.SchemeName)]
  [HttpPost("heartbeat")]
  public async Task<ActionResult<ApiResponse<object?>>> Heartbeat(
    HermesHeartbeatRequest request,
    CancellationToken cancellationToken)
  {
    var validationError = ValidateHeartbeatRequest(request);
    if (validationError is not null)
      return BadRequest(ApiResponseFactory.Failure("Heartbeat không hợp lệ.", "invalid_heartbeat", validationError));

    await hermesAgentService.RecordHeartbeatAsync(request, cancellationToken);
    return Ok(ApiResponseFactory.Success<object?>(null, "Đã ghi nhận heartbeat Hermes."));
  }

  private async Task WriteChunkAsync(HermesStreamChunk chunk, CancellationToken cancellationToken)
  {
    var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
  }

  private static string? ValidateChatRequest(HermesChatRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Message)) return "Tin nhắn không được để trống.";
    return request.Message.Length > 4000 ? "Tin nhắn quá dài. Tối đa 4000 ký tự." : null;
  }

  private static string? ValidateHeartbeatRequest(HermesHeartbeatRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.RunnerName)) return "Thiếu tên runner.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Thiếu trạng thái runner.";
    if (request.RunnerName.Length > 120) return "Tên runner quá dài.";
    if (request.Status.Length > 80) return "Trạng thái quá dài.";
    return null;
  }

  private Guid? GetAdminUserId()
  {
    var sid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    return sid is not null && Guid.TryParse(sid, out var id) ? id : null;
  }
}
