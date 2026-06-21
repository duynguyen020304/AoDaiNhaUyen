using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/users/me/orders")]
[Authorize(Policy = "RequireAdminOrCustomer")]
public sealed class UserOrderController(
    IUserService userService,
    IOrderService orderService,
    AppDbContext dbContext,
    ICacheInvalidationService cacheInvalidation,
    ILogger<UserOrderController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
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

    [HttpPatch("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        // Verify order belongs to the user
        var order = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

        if (order is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy đơn hàng",
                "order_not_found",
                "Đơn hàng không tồn tại hoặc không thuộc về bạn."));
        }

        var result = await orderService.CancelOrderAsync(orderId, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponseFactory.Failure(
                result.ErrorMessage ?? "Không thể hủy đơn hàng.",
                result.ErrorCode ?? "cancel_failed",
                result.ErrorMessage ?? "Lỗi hủy đơn hàng."));
        }

        await cacheInvalidation.InvalidateOrderRelatedCacheAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(result, "Hủy đơn hàng thành công."));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
