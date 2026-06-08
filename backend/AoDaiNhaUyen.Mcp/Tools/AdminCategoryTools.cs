using System.ComponentModel;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Mcp.Auth;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminCategoryTools
{
  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Liệt kê tất cả danh mục admin. Dùng để tìm categoryId trước khi tạo/cập nhật sản phẩm hoặc khi admin hỏi cấu trúc catalog. Không dùng để kết luận sản phẩm trong danh mục rỗng; để kiểm tra sản phẩm hãy dùng list_products/search.")]
  public static async Task<string> ListCategories(
    CancellationToken cancellationToken = default,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return ToolJson.ServiceMissing("categories");
    var cats = await categories.GetAllAsync(false, cancellationToken);
    return ToolJson.Ok(cats);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy chi tiết một danh mục.")]
  public static async Task<string> GetCategory(
    [Description("ID danh mục (GUID)")] string id,
    CancellationToken cancellationToken = default,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return ToolJson.ServiceMissing("categories");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");
    var c = await categories.GetByIdAsync(gid, cancellationToken);
    return c is null ? ToolJson.Error("Không tìm thấy danh mục.", "not_found") : ToolJson.Ok(c);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Write), Description("Tạo danh mục mới.")]
  public static async Task<string> CreateCategory(
    [Description("Tên danh mục")] string name,
    [Description("Mô tả danh mục (tùy chọn)")] string? description = null,
    CancellationToken cancellationToken = default,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return ToolJson.ServiceMissing("categories");
    if (!ToolValidation.TryRequiredName(name, out var cleanName, out var error))
      return ToolJson.Error(error!, "invalid_name");

    var dto = new CreateCategoryRequest
    {
      Name = cleanName,
      Slug = ToolValidation.Slugify(cleanName) + "-" + Random.Shared.Next(1000, 9999),
      Description = ToolValidation.Description(description)
    };
    var result = await categories.CreateAsync(dto, cancellationToken);
    return ToolJson.Ok(result);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Write), Description("Cập nhật danh mục hiện có.")]
  public static async Task<string> UpdateCategory(
    [Description("ID danh mục (GUID)")] string id,
    [Description("Tên mới (tùy chọn)")] string? name = null,
    [Description("Mô tả mới (tùy chọn)")] string? description = null,
    CancellationToken cancellationToken = default,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return ToolJson.ServiceMissing("categories");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");

    var existing = await categories.GetByIdAsync(gid, cancellationToken);
    if (existing is null) return ToolJson.Error("Không tìm thấy danh mục.", "not_found");

    var cleanName = name is null ? existing.Name : name.Trim();
    if (name is not null && !ToolValidation.TryRequiredName(cleanName, out cleanName, out var error))
      return ToolJson.Error(error!, "invalid_name");

    var dto = new UpdateCategoryRequest
    {
      Name = cleanName,
      Slug = name is not null ? ToolValidation.Slugify(cleanName) + "-" + Random.Shared.Next(1000, 9999) : existing.Slug,
      Description = ToolValidation.Description(description) ?? existing.Description
    };
    var result = await categories.UpdateAsync(gid, dto, cancellationToken);
    return result is null ? ToolJson.Error("Không tìm thấy danh mục.", "not_found") : ToolJson.Ok(result);
  }
}
