using System.ComponentModel;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminProductTools
{
  [McpServerTool, Description("Liệt kê danh sách sản phẩm với phân trang và lọc.")]
  public static async Task<string> ListProducts(
    [Description("Trang hiện tại, mặc định 1")] int page = 1,
    [Description("Số sản phẩm mỗi trang, mặc định 20")] int pageSize = 20,
    [Description("Từ khóa tìm kiếm (tùy chọn)")] string? search = null,
    [Description("Lọc theo trạng thái (tùy chọn)")] string? status = null,
    IAdminProductService? products = null)
  {
    if (products is null) return Err("products");
    var (items, total) = await products.GetPagedAsync(search, status, page, pageSize, false, CancellationToken.None);
    return JsonSerializer.Serialize(new { items, total, page, pageSize });
  }

  [McpServerTool, Description("Lấy chi tiết một sản phẩm.")]
  public static async Task<string> GetProduct(
    [Description("ID của sản phẩm (GUID)")] string id,
    IAdminProductService? products = null)
  {
    if (products is null) return Err("products");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";
    var p = await products.GetByIdAsync(gid, CancellationToken.None);
    return p is null ? "{\"error\": \"Không tìm thấy sản phẩm.\"}" : JsonSerializer.Serialize(p);
  }

  [McpServerTool, Description("Tạo sản phẩm mới (bản nháp).")]
  public static async Task<string> CreateProduct(
    [Description("Tên sản phẩm")] string name,
    [Description("Loại: ao_dai hoặc phu_kien. Mặc định: ao_dai")] string? productType = "ao_dai",
    [Description("Mô tả sản phẩm (tùy chọn)")] string? description = null,
    [Description("ID danh mục (GUID) (tùy chọn)")] string? categoryId = null,
    IAdminProductService? products = null,
    IAdminCategoryService? categories = null)
  {
    if (products is null) return Err("products");

    var slug = $"{Slugify(name)}-{Random.Shared.Next(1000, 9999)}";
    var cid = categoryId is not null && Guid.TryParse(categoryId, out var parsed)
      ? parsed
      : (categories is not null ? (await categories.GetAllAsync(false, CancellationToken.None)).FirstOrDefault()?.Id ?? Guid.Empty : Guid.Empty);

    if (cid == Guid.Empty) return "{\"error\": \"Cần ID danh mục hợp lệ.\"}";

    var dto = new CreateProductRequest
    {
      Name = name,
      Slug = slug,
      ProductType = productType ?? "ao_dai",
      CategoryId = cid,
      Description = description,
      Status = "draft"
    };

    var result = await products.CreateAsync(dto, CancellationToken.None);
    return JsonSerializer.Serialize(result);
  }

  [McpServerTool, Description("Cập nhật sản phẩm hiện có.")]
  public static async Task<string> UpdateProduct(
    [Description("ID sản phẩm (GUID)")] string id,
    [Description("Tên mới (tùy chọn)")] string? name = null,
    [Description("Mô tả mới (tùy chọn)")] string? description = null,
    IAdminProductService? products = null)
  {
    if (products is null) return Err("products");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";

    var existing = await products.GetByIdAsync(gid, CancellationToken.None);
    if (existing is null) return "{\"error\": \"Không tìm thấy sản phẩm.\"}";

    var dto = new UpdateProductRequest
    {
      Name = name ?? existing.Name,
      Slug = name is not null ? $"{Slugify(name)}-{Random.Shared.Next(1000, 9999)}" : existing.Slug,
      ProductType = existing.ProductType,
      CategoryId = existing.CategoryId,
      Description = description ?? existing.Description,
      Status = existing.Status
    };

    var result = await products.UpdateAsync(gid, dto, CancellationToken.None);
    return result is null ? "{\"error\": \"Không tìm thấy sản phẩm.\"}" : JsonSerializer.Serialize(result);
  }

  [McpServerTool, Description("Đổi trạng thái sản phẩm (active/inactive).")]
  public static async Task<string> ToggleProductStatus(
    [Description("ID sản phẩm (GUID)")] string id,
    [Description("Trạng thái mới: active hoặc inactive")] string status,
    IAdminProductService? products = null)
  {
    if (products is null) return Err("products");
    if (!Guid.TryParse(id, out var gid)) return "{\"error\": \"ID không hợp lệ.\"}";
    var ok = await products.ToggleStatusAsync(gid, status, CancellationToken.None);
    return JsonSerializer.Serialize(new { success = ok });
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
