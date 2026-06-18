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
  IHermesEventOutboxService hermesEventOutboxService,
  IHermesMonitorLinkService hermesMonitorLinkService,
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

  /// <summary>List saved Hermes reports.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpGet("reports")]
  public async Task<ActionResult<PaginatedApiResponse<IReadOnlyList<HermesReportListItemResponse>>>> ListReports(
    [FromQuery] HermesReportSearchRequest request,
    CancellationToken cancellationToken)
  {
    try
    {
      var result = await hermesAgentService.ListReportsAsync(request, cancellationToken);
      return Ok(ApiResponseFactory.PaginatedSuccess(
        result.Items,
        result.Page,
        result.PageSize,
        result.TotalCount,
        "Lấy báo cáo Hermes thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Bộ lọc báo cáo Hermes không hợp lệ.", "invalid_hermes_report_filter", ex.Message));
    }
  }

  /// <summary>Get saved Hermes report detail.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpGet("reports/{id:guid}")]
  public async Task<ActionResult<ApiResponse<HermesReportResponse>>> GetReport(Guid id, CancellationToken cancellationToken)
  {
    var report = await hermesAgentService.GetReportAsync(id, cancellationToken);
    return report is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy báo cáo Hermes.", "not_found", "Báo cáo không tồn tại."))
      : Ok(ApiResponseFactory.Success(report, "Lấy chi tiết báo cáo Hermes thành công."));
  }

  /// <summary>List Hermes outbox events.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpGet("events")]
  public async Task<ActionResult<PaginatedApiResponse<IReadOnlyList<HermesEventOutboxListItemResponse>>>> ListEvents(
    [FromQuery] HermesEventOutboxSearchRequest request,
    CancellationToken cancellationToken)
  {
    try
    {
      var result = await hermesEventOutboxService.ListEventsAsync(request, cancellationToken);
      return Ok(ApiResponseFactory.PaginatedSuccess(
        result.Items,
        result.Page,
        result.PageSize,
        result.TotalCount,
        "Lấy danh sách event Hermes thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Bộ lọc event Hermes không hợp lệ.", "invalid_hermes_event_filter", ex.Message));
    }
  }

  /// <summary>Get Hermes outbox event detail.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpGet("events/{id:guid}")]
  public async Task<ActionResult<ApiResponse<HermesEventOutboxResponse>>> GetEvent(Guid id, CancellationToken cancellationToken)
  {
    var item = await hermesEventOutboxService.GetEventAsync(id, cancellationToken);
    return item is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy event Hermes.", "not_found", "Event không tồn tại."))
      : Ok(ApiResponseFactory.Success(item, "Lấy chi tiết event Hermes thành công."));
  }

  /// <summary>Retry a failed/dead/cancelled Hermes outbox event.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpPost("events/{id:guid}/retry")]
  public async Task<ActionResult<ApiResponse<object?>>> RetryEvent(Guid id, CancellationToken cancellationToken)
  {
    var ok = await hermesEventOutboxService.RetryEventAsync(id, cancellationToken);
    return ok
      ? Ok(ApiResponseFactory.Success<object?>(null, "Đã đưa event Hermes vào hàng đợi xử lý lại."))
      : BadRequest(ApiResponseFactory.Failure("Không thể retry event Hermes.", "cannot_retry_hermes_event", "Event không tồn tại hoặc đang pending/processing."));
  }

  /// <summary>Cancel a pending/failed Hermes outbox event.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpPost("events/{id:guid}/cancel")]
  public async Task<ActionResult<ApiResponse<object?>>> CancelEvent(Guid id, CancellationToken cancellationToken)
  {
    var ok = await hermesEventOutboxService.CancelEventAsync(id, cancellationToken);
    return ok
      ? Ok(ApiResponseFactory.Success<object?>(null, "Đã hủy event Hermes."))
      : BadRequest(ApiResponseFactory.Failure("Không thể hủy event Hermes.", "cannot_cancel_hermes_event", "Event không tồn tại hoặc đã completed/dead."));
  }

  /// <summary>Create a signed public read-only monitor link for one Hermes event.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpPost("monitor-links")]
  public async Task<ActionResult<ApiResponse<HermesMonitorLinkResponse>>> CreateMonitorLink(
    CreateHermesMonitorLinkRequest request,
    CancellationToken cancellationToken)
  {
    try
    {
      var link = await hermesMonitorLinkService.CreateLinkAsync(request, GetAdminUserId(), GetPublicBaseUrl(), cancellationToken);
      return Ok(ApiResponseFactory.Success(link, "Đã tạo link giám sát Hermes."));
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(ApiResponseFactory.Failure("Không tạo được link giám sát.", "not_found", ex.Message));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Không tạo được link giám sát.", "invalid_monitor_link", ex.Message));
    }
  }

  /// <summary>Revoke a public monitor link.</summary>
  [Authorize(Policy = "RequireAdminRole")]
  [HttpPost("monitor-links/{id:guid}/revoke")]
  public async Task<ActionResult<ApiResponse<object?>>> RevokeMonitorLink(Guid id, CancellationToken cancellationToken)
  {
    var ok = await hermesMonitorLinkService.RevokeLinkAsync(id, cancellationToken);
    return ok
      ? Ok(ApiResponseFactory.Success<object?>(null, "Đã thu hồi link giám sát Hermes."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy link giám sát.", "not_found", "Link không tồn tại hoặc đã bị thu hồi."));
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

  /// <summary>Record a Hermes report. Called by Hermes runner/tooling.</summary>
  [Authorize(AuthenticationSchemes = HermesAdminAuthOptions.SchemeName)]
  [HttpPost("report")]
  public async Task<ActionResult<ApiResponse<HermesReportResponse>>> RecordReport(
    HermesReportRequest request,
    CancellationToken cancellationToken)
  {
    var validationError = ValidateReportRequest(request);
    if (validationError is not null)
      return BadRequest(ApiResponseFactory.Failure("Báo cáo Hermes không hợp lệ.", "invalid_hermes_report", validationError));

    try
    {
      var report = await hermesAgentService.RecordReportAsync(request, cancellationToken);
      return Ok(ApiResponseFactory.Success(report, "Đã lưu báo cáo Hermes."));
    }
    catch (JsonException)
    {
      return BadRequest(ApiResponseFactory.Failure("Báo cáo Hermes không hợp lệ.", "invalid_payload_json", "PayloadJson phải là JSON hợp lệ."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Báo cáo Hermes không hợp lệ.", "invalid_hermes_report", ex.Message));
    }
  }

  private async Task WriteChunkAsync(HermesStreamChunk chunk, CancellationToken cancellationToken)
  {
    var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
  }

  private static string? ValidateChatRequest(HermesChatRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Message)) return "Tin nhắn không được để trống.";
    return null;
  }

  private static string? ValidateHeartbeatRequest(HermesHeartbeatRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.RunnerName)) return "Thiếu tên runner.";
    if (string.IsNullOrWhiteSpace(request.Status)) return "Thiếu trạng thái runner.";
    if (request.RunnerName.Length > 120) return "Tên runner quá dài.";
    if (request.Status.Length > 80) return "Trạng thái quá dài.";
    return null;
  }

  private static string? ValidateReportRequest(HermesReportRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.ReportType)) return "Thiếu loại báo cáo.";
    if (string.IsNullOrWhiteSpace(request.Title)) return "Thiếu tiêu đề báo cáo.";
    if (string.IsNullOrWhiteSpace(request.Summary)) return "Thiếu tóm tắt báo cáo.";
    if (request.ReportType.Length > 80) return "Loại báo cáo quá dài.";
    if (request.Title.Length > 200) return "Tiêu đề báo cáo quá dài.";
    if (request.CorrelationId?.Length > 128) return "CorrelationId quá dài.";
    return null;
  }

  private string GetPublicBaseUrl()
  {
    var origin = Request.Headers.Origin.ToString();
    if (Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
      return originUri.GetLeftPart(UriPartial.Authority);

    return $"{Request.Scheme}://{Request.Host}";
  }

  private Guid? GetAdminUserId()
  {
    var sid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    return sid is not null && Guid.TryParse(sid, out var id) ? id : null;
  }
}
