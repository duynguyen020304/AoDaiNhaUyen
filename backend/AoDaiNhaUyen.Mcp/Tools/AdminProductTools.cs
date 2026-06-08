using System.ComponentModel;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Mcp.Auth;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AoDaiNhaUyen.Mcp.Tools;

[McpServerToolType]
public static class AdminProductTools
{
  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Liệt kê danh sách sản phẩm với phân trang và lọc.")]
  public static async Task<string> ListProducts(
    [Description("Trang hiện tại, mặc định 1")] int page = 1,
    [Description("Số sản phẩm mỗi trang, mặc định 20")] int pageSize = 20,
    [Description("Từ khóa tìm kiếm (tùy chọn)")] string? search = null,
    [Description("Lọc theo trạng thái (tùy chọn)")] string? status = null,
    CancellationToken cancellationToken = default,
    IAdminProductService? products = null)
  {
    if (products is null) return ToolJson.ServiceMissing("products");
    var cleanStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
    if (cleanStatus is not null && !ToolValidation.IsProductStatus(cleanStatus))
      return ToolJson.Error("Trạng thái phải là draft, active hoặc inactive.", "invalid_status");

    page = ToolValidation.Page(page);
    pageSize = ToolValidation.PageSize(pageSize);
    var (items, total) = await products.GetPagedAsync(
      ToolValidation.Search(search), cleanStatus, page, pageSize, false, cancellationToken);
    return ToolJson.Ok(new { items, total, page, pageSize });
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Read), Description("Lấy chi tiết một sản phẩm.")]
  public static async Task<string> GetProduct(
    [Description("ID của sản phẩm (GUID)")] string id,
    CancellationToken cancellationToken = default,
    IAdminProductService? products = null)
  {
    if (products is null) return ToolJson.ServiceMissing("products");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");
    var p = await products.GetByIdAsync(gid, cancellationToken);
    return p is null ? ToolJson.Error("Không tìm thấy sản phẩm.", "not_found") : ToolJson.Ok(p);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Write), Description("Tạo sản phẩm mới (bản nháp).")]
  public static async Task<string> CreateProduct(
    [Description("Tên sản phẩm")] string name,
    [Description("Loại: ao_dai hoặc phu_kien. Mặc định: ao_dai")] string? productType = "ao_dai",
    [Description("Mô tả sản phẩm (tùy chọn)")] string? description = null,
    [Description("ID danh mục (GUID) (tùy chọn)")] string? categoryId = null,
    CancellationToken cancellationToken = default,
    IAdminProductService? products = null,
    IAdminCategoryService? categories = null)
  {
    if (products is null) return ToolJson.ServiceMissing("products");
    if (!ToolValidation.TryRequiredName(name, out var cleanName, out var nameError))
      return ToolJson.Error(nameError!, "invalid_name");

    var cleanProductType = string.IsNullOrWhiteSpace(productType) ? "ao_dai" : productType.Trim().ToLowerInvariant();
    if (!ToolValidation.IsProductType(cleanProductType))
      return ToolJson.Error("Loại sản phẩm phải là ao_dai hoặc phu_kien.", "invalid_product_type");

    var cid = categoryId is not null && Guid.TryParse(categoryId, out var parsed)
      ? parsed
      : (categories is not null ? (await categories.GetAllAsync(false, cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty : Guid.Empty);

    if (cid == Guid.Empty) return ToolJson.Error("Cần ID danh mục hợp lệ.", "invalid_category_id");

    var dto = new CreateProductRequest
    {
      Name = cleanName,
      Slug = $"{ToolValidation.Slugify(cleanName)}-{Random.Shared.Next(1000, 9999)}",
      ProductType = cleanProductType,
      CategoryId = cid,
      Description = ToolValidation.Description(description),
      Status = "draft"
    };

    var result = await products.CreateAsync(dto, cancellationToken);
    return ToolJson.Ok(result);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Write), Description("Cập nhật sản phẩm hiện có.")]
  public static async Task<string> UpdateProduct(
    [Description("ID sản phẩm (GUID)")] string id,
    [Description("Tên mới (tùy chọn)")] string? name = null,
    [Description("Mô tả mới (tùy chọn)")] string? description = null,
    CancellationToken cancellationToken = default,
    IAdminProductService? products = null)
  {
    if (products is null) return ToolJson.ServiceMissing("products");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");

    var existing = await products.GetByIdAsync(gid, cancellationToken);
    if (existing is null) return ToolJson.Error("Không tìm thấy sản phẩm.", "not_found");

    var cleanName = name is null ? existing.Name : name.Trim();
    if (name is not null && !ToolValidation.TryRequiredName(cleanName, out cleanName, out var nameError))
      return ToolJson.Error(nameError!, "invalid_name");

    var dto = new UpdateProductRequest
    {
      Name = cleanName,
      Slug = name is not null ? $"{ToolValidation.Slugify(cleanName)}-{Random.Shared.Next(1000, 9999)}" : existing.Slug,
      ProductType = existing.ProductType,
      CategoryId = existing.CategoryId,
      Description = ToolValidation.Description(description) ?? existing.Description,
      Status = existing.Status
    };

    var result = await products.UpdateAsync(gid, dto, cancellationToken);
    return result is null ? ToolJson.Error("Không tìm thấy sản phẩm.", "not_found") : ToolJson.Ok(result);
  }

  [McpServerTool, Authorize(Policy = McpPolicies.Write), Description("Đổi trạng thái sản phẩm (active/inactive).")]
  public static async Task<string> ToggleProductStatus(
    [Description("ID sản phẩm (GUID)")] string id,
    [Description("Trạng thái mới: active hoặc inactive")] string status,
    CancellationToken cancellationToken = default,
    IAdminProductService? products = null)
  {
    if (products is null) return ToolJson.ServiceMissing("products");
    if (!Guid.TryParse(id, out var gid)) return ToolJson.Error("ID không hợp lệ.", "invalid_id");
    if (!ToolValidation.IsActiveStatus(status)) return ToolJson.Error("Trạng thái phải là active hoặc inactive.", "invalid_status");

    var ok = await products.ToggleStatusAsync(gid, status.ToLowerInvariant(), cancellationToken);
    return ToolJson.Ok(new { success = ok });
  }
}
