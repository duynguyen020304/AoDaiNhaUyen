using System.Reflection;
using AoDaiNhaUyen.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/cache")]
public sealed class CacheController(IConfiguration configuration) : ControllerBase
{
  [HttpGet("version")]
  public IActionResult GetVersion()
  {
    Response.Headers.CacheControl = "no-store";

    var version = configuration["CacheSettings:Version"]
      ?? configuration["CacheVersion"]
      ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
      ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
      ?? "v1";

    return Ok(ApiResponseFactory.Success(new { version }));
  }
}
