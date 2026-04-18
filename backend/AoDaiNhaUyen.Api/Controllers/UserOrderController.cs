using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/users/me/orders")]
[Authorize]
public sealed class UserOrderController(
    IUserService userService,
    ILogger<UserOrderController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} requested orders page {Page} with {PageSize} items", userId, page, pageSize);

        var result = await userService.GetUserOrdersAsync(userId, page, pageSize, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Lấy đơn hàng thất bại",
                result.ErrorCode ?? "orders_not_found",
                result.ErrorMessage ?? "Không thể lấy lịch sử đơn hàng."));
        }

        return Ok(ApiResponseFactory.PaginatedSuccess(
            result.Value!.Items,
            result.Value.Page,
            result.Value.PageSize,
            result.Value.TotalCount));
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
