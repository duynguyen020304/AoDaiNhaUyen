using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin user management endpoints.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    /// <summary>Get a paginated list of all users for admin.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await adminUserService.GetUsersAsync(search, page, pageSize, includeDeleted, cancellationToken);

        return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, page, pageSize, result.TotalCount));
    }

    /// <summary>Get a single user by ID for admin editing.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminUserListItemDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await adminUserService.GetUserByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy người dùng.",
                "not_found",
                "Người dùng không tồn tại hoặc đã bị xóa."));
        }

        return Ok(ApiResponseFactory.Success(user));
    }

    /// <summary>Create a new user.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminUserListItemDto>>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await adminUserService.CreateUserAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            ApiResponseFactory.Success(user, "Tạo người dùng thành công."));
    }

    /// <summary>Update an existing user.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminUserListItemDto>>> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await adminUserService.UpdateUserAsync(id, request, cancellationToken);

        if (user is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy người dùng.",
                "not_found",
                "Người dùng không tồn tại hoặc đã bị xóa."));
        }

        return Ok(ApiResponseFactory.Success(user, "Cập nhật người dùng thành công."));
    }

    /// <summary>Update user role.</summary>
    [HttpPatch("{id:guid}/role")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await adminUserService.UpdateUserRoleAsync(id, request, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy người dùng hoặc vai trò không hợp lệ.",
                "not_found",
                "Không thể cập nhật vai trò."));
        }

        return Ok(ApiResponseFactory.Success<object?>(null, "Cập nhật vai trò thành công."));
    }

    /// <summary>Update user status.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        Guid id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await adminUserService.UpdateUserStatusAsync(id, request, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy người dùng.",
                "not_found",
                "Không thể cập nhật trạng thái."));
        }

        return Ok(ApiResponseFactory.Success<object?>(null, "Cập nhật trạng thái thành công."));
    }

    /// <summary>Soft-delete a user.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminUserService.DeleteUserAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy người dùng.",
                "not_found",
                "Người dùng không tồn tại hoặc đã bị xóa."));
        }

        return NoContent();
    }

    /// <summary>Restore a soft-deleted user.</summary>
    [HttpPatch("{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminUserService.RestoreUserAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy người dùng.",
                "not_found",
                "Người dùng không tồn tại hoặc chưa bị xóa."));
        }

        return Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục người dùng thành công."));
    }
}