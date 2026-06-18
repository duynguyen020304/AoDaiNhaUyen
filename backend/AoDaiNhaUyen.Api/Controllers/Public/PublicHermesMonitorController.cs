using System.Text.Json;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AoDaiNhaUyen.Api.Controllers.Public;

/// <summary>Public read-only Hermes monitor endpoints backed by signed bearer URLs.</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("hermes-monitor")]
[Route("api/public/hermes/monitor")]
public sealed class PublicHermesMonitorController(IHermesMonitorLinkService monitorLinkService) : ControllerBase
{
  /// <summary>Get a sanitized Hermes monitor snapshot for a signed token.</summary>
  [HttpGet("{token}")]
  public async Task<ActionResult<ApiResponse<HermesMonitorSnapshotResponse>>> GetSnapshot(string token, CancellationToken cancellationToken)
  {
    var snapshot = await monitorLinkService.GetSnapshotAsync(token, cancellationToken);
    return snapshot is null
      ? NotFound(ApiResponseFactory.Failure("Link giám sát không hợp lệ hoặc đã hết hạn.", "invalid_monitor_token", "Token không tồn tại, đã hết hạn hoặc đã bị thu hồi."))
      : Ok(ApiResponseFactory.Success(snapshot, "Lấy dữ liệu giám sát Hermes thành công."));
  }

  /// <summary>Stream sanitized Hermes monitor snapshots via Server-Sent Events.</summary>
  [HttpGet("{token}/stream")]
  public async Task Stream(string token, CancellationToken cancellationToken)
  {
    Response.ContentType = "text/event-stream";
    Response.Headers.Append("Cache-Control", "no-cache");
    Response.Headers.Append("Connection", "keep-alive");
    Response.Headers.Append("X-Accel-Buffering", "no");

    var sentAny = false;
    for (var i = 0; i < 60 && !cancellationToken.IsCancellationRequested; i++)
    {
      var snapshot = await monitorLinkService.GetSnapshotAsync(token, cancellationToken);
      if (snapshot is null)
      {
        await WriteEventAsync("error", new { message = "Link giám sát không hợp lệ hoặc đã hết hạn." }, cancellationToken);
        break;
      }

      await WriteEventAsync(sentAny ? "snapshot" : "snapshot", snapshot, cancellationToken);
      sentAny = true;

      if (snapshot.Event.Status is "completed" or "dead" or "cancelled")
      {
        await WriteEventAsync("completed", new { status = snapshot.Event.Status }, cancellationToken);
        break;
      }

      await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    await Response.WriteAsync("event: done\ndata: [DONE]\n\n", cancellationToken);
  }

  private async Task WriteEventAsync(string eventName, object payload, CancellationToken cancellationToken)
  {
    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
    await Response.Body.FlushAsync(cancellationToken);
  }
}
