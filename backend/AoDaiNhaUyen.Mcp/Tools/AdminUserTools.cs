using System.ComponentModel;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminUserTools
{
  [McpServerTool, Description("Liệt kê danh sách người dùng với phân trang và tìm kiếm.")]
  public static async Task<string> ListUsers(
    [Description("Trang hiện tại, mặc định 1")] int page = 1,
    [Description("Số người dùng mỗi trang, mặc định 20")] int pageSize = 20,
    [Description("Từ khóa tìm kiếm (tùy chọn)")] string? search = null,
    IAdminUserService? users = null)
  {
    if (users is null) return Err("users");
    var result = await users.GetUsersAsync(search, page, pageSize, false, CancellationToken.None);
    return JsonSerializer.Serialize(result);
  }

  [McpServerTool, Description("Lấy chi tiết một người dùng.")]
  public static async Task<string> GetUser(
    [Description("ID người dùng (GUID)")] string id,
    IAdminUserService? users = null)
  {
    if (users is null) return Err("users");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";
    var u = await users.GetUserByIdAsync(gid, CancellationToken.None);
    return u is null ? "{\"error\": \"Không tìm thấy người dùng.\"}" : JsonSerializer.Serialize(u);
  }

  [McpServerTool, Description("Bật/tắt trạng thái người dùng (active/inactive).")]
  public static async Task<string> UpdateUserStatus(
    [Description("ID người dùng (GUID)")] string id,
    [Description("Trạng thái mới: active hoặc inactive")] string status,
    IAdminUserService? users = null)
  {
    if (users is null) return Err("users");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";
    var ok = await users.UpdateUserStatusAsync(gid,
      new UpdateUserStatusRequest { Status = status }, CancellationToken.None);
    return JsonSerializer.Serialize(new { success = ok });
  }

  [McpServerTool, Description("Thay đổi vai trò người dùng.")]
  public static async Task<string> UpdateUserRole(
    [Description("ID người dùng (GUID)")] string id,
    [Description("Vai trò mới: admin hoặc customer")] string role,
    IAdminUserService? users = null,
    IAdminRoleService? roles = null)
  {
    if (users is null) return Err("users");
    if (roles is null) return Err("roles");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";

    // Look up role ID by name
    var allRoles = await roles.GetRolesAsync(CancellationToken.None);
    var targetRole = allRoles.FirstOrDefault(r =>
      r.Name.Equals(role, StringComparison.OrdinalIgnoreCase));
    if (targetRole is null)
      return $"{{\"error\": \"Không tìm thấy vai trò '{role}'.\"}}";

    var ok = await users.UpdateUserRoleAsync(gid,
      new UpdateUserRoleRequest { RoleId = targetRole.Id }, CancellationToken.None);
    return JsonSerializer.Serialize(new { success = ok });
  }

  private static string Err(string svc) => $"{{\"error\": \"{svc} service chưa được inject.\"}}";
}
