using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(ICatalogService catalogService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Get(
    [FromQuery] string? categorySlug,
    [FromQuery] string? productType,
    [FromQuery] bool? featured,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    CancellationToken cancellationToken = default)
  {
    var result = await catalogService.GetProductsAsync(
      categorySlug,
      productType,
      featured,
      page,
      pageSize,
      cancellationToken);

    return Ok(new
    {
      data = result.Items,
      meta = new
      {
        total = result.TotalCount,
        page = result.Page,
        pageSize = result.PageSize,
        totalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
      }
    });
  }

  [HttpGet("{slug}")]
  public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
  {
    var product = await catalogService.GetProductBySlugAsync(slug, cancellationToken);
    if (product is null)
    {
      return NotFound(new
      {
        error = new
        {
          code = "not_found",
          message = "Product not found."
        }
      });
    }

    return Ok(new { data = product });
  }
}
