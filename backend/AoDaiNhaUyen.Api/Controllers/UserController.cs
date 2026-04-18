using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.User;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/users/me")]
[Authorize]
public sealed class UserController(
    IUserService userService,
    ILogger<UserController> logger) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} requested profile", userId);

        var result = await userService.GetUserProfileAsync(userId, cancellationToken);

        if (!result.Succeeded || result.Value == null)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Lấy thông tin thất bại",
                result.ErrorCode ?? "profile_not_found",
                result.ErrorMessage ?? "Không thể lấy thông tin người dùng."));
        }

        return Ok(ApiResponseFactory.Success(result.Value));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileDto profile, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(ApiResponseFactory.Failure(
                "Không có quyền truy cập",
                "unauthorized",
                "Vui lòng đăng nhập."));
        }

        logger.LogInformation("User {UserId} updating profile", userId);

        var result = await userService.UpdateUserProfileAsync(userId, profile, cancellationToken);

        if (!result.Succeeded || result.Value == null)
        {
            return BadRequest(ApiResponseFactory.Failure(
                "Cập nhật thất bại",
                result.ErrorCode ?? "update_failed",
                result.ErrorMessage ?? "Không thể cập nhật thông tin người dùng."));
        }

        return Ok(ApiResponseFactory.Success(result.Value, "Cập nhật hồ sơ thành công."));
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}