using System.Text.Json;
using System.Security.Claims;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(ICustomerEventService customerEventService) : ControllerBase
{
  [EnableRateLimiting("chat")]
  [HttpPost]
  public async Task<IActionResult> Track([FromBody] TrackCustomerEventRequest request, CancellationToken cancellationToken)
  {
    try
    {
      var result = await customerEventService.TrackAsync(
        GetCurrentUserIdOrNull(),
        request,
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(),
        cancellationToken);

      return Ok(ApiResponseFactory.Success(result, "Ghi nhận sự kiện thành công."));
    }
    catch (JsonException)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu sự kiện không hợp lệ.", "invalid_metadata", "MetadataJson phải là JSON hợp lệ."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu sự kiện không hợp lệ.", "invalid_event", ex.Message));
    }
  }

  private Guid? GetCurrentUserIdOrNull()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
  }
}
