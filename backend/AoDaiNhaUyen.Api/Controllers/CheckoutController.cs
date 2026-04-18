using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Checkout;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/users/me/checkout")]
[Authorize]
public sealed class CheckoutController(ICheckoutService checkoutService) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request, CancellationToken cancellationToken)
  {
    var result = await checkoutService.CheckoutAsync(GetCurrentUserId(), request, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
      return BadRequest(ApiResponseFactory.Failure("Thanh toan that bai", result.ErrorCode ?? "checkout_failed", result.ErrorMessage ?? "Khong the tao don hang."));
    }

    return Ok(ApiResponseFactory.Success(result.Value, "Thanh toan thanh cong."));
  }

  private long GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return long.TryParse(userIdClaim, out var userId) ? userId : 0;
  }
}
