using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
  [HttpGet]
  public IActionResult Get()
  {
    return Ok(new
    {
      status = "ok",
      timestampUtc = DateTime.UtcNow
    });
  }
}
