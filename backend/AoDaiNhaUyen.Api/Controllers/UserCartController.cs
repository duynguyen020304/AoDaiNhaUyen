using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Cart;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/users/me/cart")]
[Authorize]
public sealed class UserCartController(ICartService cartService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
  {
    var result = await cartService.GetCartAsync(GetCurrentUserId(), cancellationToken);
    return Ok(ApiResponseFactory.Success(result.Value!));
  }

  [HttpPost("items")]
  public async Task<IActionResult> AddItem([FromBody] AddCartItemDto request, CancellationToken cancellationToken)
  {
    var result = await cartService.AddItemAsync(GetCurrentUserId(), request, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
      return BadRequest(ApiResponseFactory.Failure("Thao tac gio hang that bai", result.ErrorCode ?? "cart_error", result.ErrorMessage ?? "Khong the cap nhat gio hang."));
    }

    return Ok(ApiResponseFactory.Success(result.Value, "Cap nhat gio hang thanh cong."));
  }

  [HttpPut("items/{itemId:long}")]
  public async Task<IActionResult> UpdateItem(long itemId, [FromBody] UpdateCartItemDto request, CancellationToken cancellationToken)
  {
    var result = await cartService.UpdateItemAsync(GetCurrentUserId(), itemId, request, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
      return BadRequest(ApiResponseFactory.Failure("Thao tac gio hang that bai", result.ErrorCode ?? "cart_error", result.ErrorMessage ?? "Khong the cap nhat gio hang."));
    }

    return Ok(ApiResponseFactory.Success(result.Value, "Cap nhat gio hang thanh cong."));
  }

  [HttpDelete("items/{itemId:long}")]
  public async Task<IActionResult> RemoveItem(long itemId, CancellationToken cancellationToken)
  {
    var result = await cartService.RemoveItemAsync(GetCurrentUserId(), itemId, cancellationToken);
    if (!result.Succeeded || result.Value is null)
    {
      return BadRequest(ApiResponseFactory.Failure("Thao tac gio hang that bai", result.ErrorCode ?? "cart_error", result.ErrorMessage ?? "Khong the xoa san pham khoi gio hang."));
    }

    return Ok(ApiResponseFactory.Success(result.Value, "Xoa san pham thanh cong."));
  }

  [HttpDelete]
  public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
  {
    var result = await cartService.ClearCartAsync(GetCurrentUserId(), cancellationToken);
    if (!result.Succeeded)
    {
      return BadRequest(ApiResponseFactory.Failure("Thao tac gio hang that bai", result.ErrorCode ?? "cart_error", result.ErrorMessage ?? "Khong the xoa gio hang."));
    }

    return Ok(ApiResponseFactory.Success(true, "Xoa gio hang thanh cong."));
  }

  private long GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return long.TryParse(userIdClaim, out var userId) ? userId : 0;
  }
}
