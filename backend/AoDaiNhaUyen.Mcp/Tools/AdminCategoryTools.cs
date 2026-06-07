using System.ComponentModel;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminCategoryTools
{
  [McpServerTool, Description("Liệt kê tất cả danh mục.")]
  public static async Task<string> ListCategories(
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return Err("categories");
    var cats = await categories.GetAllAsync(false, CancellationToken.None);
    return JsonSerializer.Serialize(cats);
  }

  [McpServerTool, Description("Lấy chi tiết một danh mục.")]
  public static async Task<string> GetCategory(
    [Description("ID danh mục (GUID)")] string id,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return Err("categories");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";
    var c = await categories.GetByIdAsync(gid, CancellationToken.None);
    return c is null ? "{\"error\": \"Không tìm thấy danh mục.\"}" : JsonSerializer.Serialize(c);
  }

  [McpServerTool, Description("Tạo danh mục mới.")]
  public static async Task<string> CreateCategory(
    [Description("Tên danh mục")] string name,
    [Description("Mô tả danh mục (tùy chọn)")] string? description = null,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return Err("categories");
    var slug = Slugify(name) + "-" + Random.Shared.Next(1000, 9999);
    var dto = new CreateCategoryRequest
    {
      Name = name,
      Slug = slug,
      Description = description
    };
    var result = await categories.CreateAsync(dto, CancellationToken.None);
    return JsonSerializer.Serialize(result);
  }

  [McpServerTool, Description("Cập nhật danh mục hiện có.")]
  public static async Task<string> UpdateCategory(
    [Description("ID danh mục (GUID)")] string id,
    [Description("Tên mới (tùy chọn)")] string? name = null,
    [Description("Mô tả mới (tùy chọn)")] string? description = null,
    IAdminCategoryService? categories = null)
  {
    if (categories is null) return Err("categories");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";

    var existing = await categories.GetByIdAsync(gid, CancellationToken.None);
    if (existing is null) return "{\"error\": \"Không tìm thấy danh mục.\"}";

    var dto = new UpdateCategoryRequest
    {
      Name = name ?? existing.Name,
      Slug = name is not null ? Slugify(name) + "-" + Random.Shared.Next(1000, 9999) : existing.Slug,
      Description = description ?? existing.Description
    };
    var result = await categories.UpdateAsync(gid, dto, CancellationToken.None);
    return result is null ? "{\"error\": \"Không tìm thấy danh mục.\"}" : JsonSerializer.Serialize(result);
  }

  private static string Err(string svc) => $"{{\"error\": \"{svc} service chưa được inject.\"}}";

  private static string Slugify(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return "untitled";
    var slug = System.Text.RegularExpressions.Regex.Replace(
      text.Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
        .Aggregate("", (s, c) => s + c),
      @"[^a-z0-9\s-]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
    slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");
    slug = slug.Trim('-').ToLowerInvariant();
    return slug.Length > 200 ? slug[..200] : slug.Length > 0 ? slug : "untitled";
  }
}
