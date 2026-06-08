using System.ComponentModel;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Mcp.Auth;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminUserTools
{
  private const string RoleChangeConfirmation = "I_UNDERSTAND_ROLE_CHANGE";

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Liệt kê danh sách người dùng với phân trang và tìm kiếm.")]
  public static async Task<string> ListUsers(
    [Description("Trang hiện tại, mặc định 1")] int page = 1,
    [Description("Số người dùng mỗi trang, mặc định 20")] int pageSize = 20,
    [Description("Từ khóa tìm kiếm (tùy chọn)")] string? search = null,
    CancellationToken cancellationToken = default,
    IAdminUserService? users = null)
  {
    if (users is null) return ToolJson.ServiceMissing("users");
    var result = await users.GetUsersAsync(
      ToolValidation.Search(search),
      ToolValidation.Page(page),
      ToolValidation.PageSize(pageSize),
      false,
      cancellationToken);
    return ToolJson.Ok(result);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy chi tiết một người dùng.")]
  public static async Task<string> GetUser(
    [Description("ID người dùng (GUID)")] string id,
    CancellationToken cancellationToken = default,
    IAdminUserService? users = null)
  {
    if (users is null) return ToolJson.ServiceMissing("users");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");
    var u = await users.GetUserByIdAsync(gid, cancellationToken);
    return u is null ? ToolJson.Error("Không tìm thấy người dùng.", "not_found") : ToolJson.Ok(u);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Users), Description("Bật/tắt trạng thái người dùng (active/inactive).")]
  public static async Task<string> UpdateUserStatus(
    [Description("ID người dùng (GUID)")] string id,
    [Description("Trạng thái mới: active hoặc inactive")] string status,
    CancellationToken cancellationToken = default,
    IAdminUserService? users = null)
  {
    if (users is null) return ToolJson.ServiceMissing("users");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");
    if (!ToolValidation.IsActiveStatus(status)) return ToolJson.Error("Trạng thái phải là active hoặc inactive.", "invalid_status");

    var ok = await users.UpdateUserStatusAsync(gid,
      new UpdateUserStatusRequest { Status = status.ToLowerInvariant() }, cancellationToken);
    return ToolJson.Ok(new { success = ok });
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Roles), Description("Thay đổi vai trò người dùng.")]
  public static async Task<string> UpdateUserRole(
    [Description("ID người dùng (GUID)")] string id,
    [Description("Vai trò mới: admin hoặc customer")] string role,
    [Description("Bắt buộc: I_UNDERSTAND_ROLE_CHANGE")] string confirm,
    CancellationToken cancellationToken = default,
    IAdminUserService? users = null,
    IAdminRoleService? roles = null)
  {
    if (users is null) return ToolJson.ServiceMissing("users");
    if (roles is null) return ToolJson.ServiceMissing("roles");
    if (confirm != RoleChangeConfirmation) return ToolJson.Error("Thiếu xác nhận đổi vai trò.", "confirmation_required");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");
    if (!ToolValidation.IsUserRole(role)) return ToolJson.Error("Vai trò phải là admin hoặc customer.", "invalid_role");

    var allRoles = await roles.GetRolesAsync(cancellationToken);
    var targetRole = allRoles.FirstOrDefault(r =>
      r.Name.Equals(role, StringComparison.OrdinalIgnoreCase));
    if (targetRole is null)
      return ToolJson.Error($"Không tìm thấy vai trò '{role}'.", "not_found");

    var ok = await users.UpdateUserRoleAsync(gid,
      new UpdateUserRoleRequest { RoleId = targetRole.Id }, cancellationToken);
    return ToolJson.Ok(new { success = ok });
  }
}
