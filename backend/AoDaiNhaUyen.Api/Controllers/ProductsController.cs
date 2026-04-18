using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Api.Responses;
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
    [FromQuery] string? size,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 12,
    CancellationToken cancellationToken = default)
  {
    var result = await catalogService.GetProductsAsync(
      categorySlug,
      productType,
      featured,
      size,
      page,
      pageSize,
      cancellationToken);

    return Ok(ApiResponseFactory.PaginatedSuccess(
      result.Items,
      result.Page,
      result.PageSize,
      result.TotalCount));
  }

  [HttpGet("{slug}")]
  public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
  {
    var product = await catalogService.GetProductBySlugAsync(slug, cancellationToken);
    if (product is null)
    {
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy dữ liệu",
        "not_found",
        "Product not found."));
    }

    return Ok(ApiResponseFactory.Success(product));
  }
}
