using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminInventoryController(IStockService stockService) : ControllerBase
{
  /// <summary>
  /// Lấy danh sách sản phẩm sắp hết hàng (stock ≤ threshold).
  /// </summary>
  [HttpGet("low-stock")]
  public async Task<IActionResult> GetLowStock(
    [FromQuery] int threshold = 5,
    CancellationToken cancellationToken = default)
  {
    var alerts = await stockService.GetLowStockAlertsAsync(threshold, cancellationToken);
    return Ok(ApiResponseFactory.Success(alerts));
  }
}
