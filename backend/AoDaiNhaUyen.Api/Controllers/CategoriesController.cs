using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(ICatalogService catalogService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Get(CancellationToken cancellationToken)
  {
    var data = await catalogService.GetCategoriesAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(data));
  }

  [HttpGet("header")]
  public async Task<IActionResult> GetHeader(CancellationToken cancellationToken)
  {
    var data = await catalogService.GetHeaderCategoriesAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(data));
  }
}
