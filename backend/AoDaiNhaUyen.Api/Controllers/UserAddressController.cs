using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/users/me/addresses")]
[Authorize(Policy = "RequireAdminOrCustomer")]
public sealed class UserAddressController(
    IUserService userService,
    ILogger<UserAddressController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAddresses(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} requested addresses", userId);

        var result = await userService.GetUserAddressesAsync(userId, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Lấy địa chỉ thất bại",
                result.ErrorCode ?? "addresses_not_found",
                result.ErrorMessage ?? "Không thể lấy danh sách địa chỉ."));
        }

        return Ok(ApiResponseFactory.Success(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto address, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} creating address", userId);

        var result = await userService.CreateUserAddressAsync(userId, address, cancellationToken);

        if (!result.Succeeded || result.Value == null)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Tạo địa chỉ thất bại",
                result.ErrorCode ?? "address_creation_failed",
                result.ErrorMessage ?? "Không thể tạo địa chỉ mới."));
        }

        return Ok(ApiResponseFactory.Success(result.Value, "Tạo địa chỉ thành công."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] CreateAddressDto address, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} updating address {AddressId}", userId, id);

        var result = await userService.UpdateUserAddressAsync(userId, id, address, cancellationToken);

        if (!result.Succeeded || result.Value == null)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Cập nhật địa chỉ thất bại",
                result.ErrorCode ?? "address_update_failed",
                result.ErrorMessage ?? "Không thể cập nhật địa chỉ này."));
        }

        return Ok(ApiResponseFactory.Success(result.Value, "Cập nhật địa chỉ thành công."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} deleting address {AddressId}", userId, id);

        var result = await userService.DeleteUserAddressAsync(userId, id, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Xóa địa chỉ thất bại",
                result.ErrorCode ?? "address_deletion_failed",
                result.ErrorMessage ?? "Không thể xóa địa chỉ này."));
        }

        return Ok(ApiResponseFactory.Success(true, "Xóa địa chỉ thành công."));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}