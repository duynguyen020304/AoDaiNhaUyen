using Microsoft.AspNetCore.RateLimiting;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/marketing")]
public sealed class MarketingController(ISubscriberService subscriberService) : ControllerBase
{
  [EnableRateLimiting("auth")]
  [HttpPost("subscribe")]
  public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken cancellationToken)
  {
    var result = await subscriberService.SubscribeAsync(
      request.Email,
      string.IsNullOrWhiteSpace(request.Source) ? "newsletter" : request.Source,
      HttpContext.Connection.RemoteIpAddress?.ToString(),
      Request.Headers.UserAgent.ToString(),
      cancellationToken);

    if (result.Status == "invalid")
    {
      return BadRequest(ApiResponseFactory.Failure(result.Message, "invalid_email", result.Message));
    }

    return Ok(ApiResponseFactory.Success(result, result.Message));
  }

  [EnableRateLimiting("auth")]
  [HttpPost("confirm")]
  public async Task<IActionResult> Confirm([FromBody] TokenRequest request, CancellationToken cancellationToken)
  {
    var result = await subscriberService.ConfirmAsync(request.Token, cancellationToken);
    if (result.Status == "invalid")
    {
      return BadRequest(ApiResponseFactory.Failure(result.Message, "invalid_token", result.Message));
    }

    return Ok(ApiResponseFactory.Success(result, result.Message));
  }

  [EnableRateLimiting("auth")]
  [HttpPost("unsubscribe")]
  public async Task<IActionResult> Unsubscribe([FromBody] TokenRequest request, CancellationToken cancellationToken)
  {
    var result = await subscriberService.UnsubscribeAsync(request.Token, cancellationToken);
    if (result.Status == "invalid")
    {
      return BadRequest(ApiResponseFactory.Failure(result.Message, "invalid_token", result.Message));
    }

    return Ok(ApiResponseFactory.Success(result, result.Message));
  }
}
