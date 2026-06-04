using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin role management endpoints.</summary>
[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminRolesController(IAdminRoleService adminRoleService) : ControllerBase
{
    /// <summary>Get a list of all roles.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetRoles(
        CancellationToken cancellationToken = default)
    {
        var roles = await adminRoleService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponseFactory.Success(roles));
    }

    /// <summary>Create a new role.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await adminRoleService.CreateRoleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRoles), ApiResponseFactory.Success(role, "Tạo vai trò thành công."));
    }

    /// <summary>Update an existing role.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await adminRoleService.UpdateRoleAsync(id, request, cancellationToken);

        if (role is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy vai trò.",
                "not_found",
                "Vai trò không tồn tại."));
        }

        return Ok(ApiResponseFactory.Success(role, "Cập nhật vai trò thành công."));
    }

    /// <summary>Delete a role.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminRoleService.DeleteRoleAsync(id, cancellationToken);

        if (!success)
        {
            return Conflict(ApiResponseFactory.Failure(
                "Không thể xóa vai trò.",
                "conflict",
                "Vai trò đang được gán cho người dùng hoặc không tồn tại."));
        }

        return NoContent();
    }
}