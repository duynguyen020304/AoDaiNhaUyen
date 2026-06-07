using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Promo;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/promo")]
[Authorize(Policy = "RequireAdminOrCustomer")]
public sealed class PromoController(IPromoService promoService) : ControllerBase
{
  /// <summary>
  /// Validate mã giảm giá trước khi checkout.
  /// </summary>
  [HttpPost("validate")]
  public async Task<IActionResult> Validate([FromBody] ApplyPromoRequest request, CancellationToken cancellationToken)
  {
    var result = await promoService.ValidateAsync(request.Code, request.Subtotal, cancellationToken);

    if (!result.IsValid)
    {
      return BadRequest(ApiResponseFactory.Failure(
        result.ErrorMessage ?? "Mã giảm giá không hợp lệ.",
        result.ErrorCode ?? "promo_invalid",
        result.ErrorMessage ?? "Không thể áp dụng mã giảm giá."));
    }

    return Ok(ApiResponseFactory.Success(result, "Mã giảm giá hợp lệ."));
  }
}
