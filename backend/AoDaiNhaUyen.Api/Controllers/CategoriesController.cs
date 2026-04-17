using AoDaiNhaUyen.Application.Interfaces.Services;
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
    return Ok(new { data });
  }
}
