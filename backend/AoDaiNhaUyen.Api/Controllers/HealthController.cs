using AoDaiNhaUyen.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
  [HttpGet]
  public IActionResult Get()
  {
    return Ok(ApiResponseFactory.Success(new
    {
      status = "ok",
      timestampUtc = DateTime.UtcNow
    }));
  }
}
