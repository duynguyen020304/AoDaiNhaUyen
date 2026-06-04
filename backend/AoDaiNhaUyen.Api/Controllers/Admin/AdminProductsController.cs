using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin product management endpoints.</summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminProductsController(IAdminProductService adminProductService) : ControllerBase
{
    /// <summary>Get a paginated list of all products for admin.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, totalCount) = await adminProductService.GetPagedAsync(search, status, page, pageSize, includeDeleted, cancellationToken);

        return Ok(ApiResponseFactory.PaginatedSuccess(items, page, pageSize, totalCount));
    }

    /// <summary>Get a single product by ID for admin editing.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminProductDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await adminProductService.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy sản phẩm.",
                "not_found",
                "Sản phẩm không tồn tại hoặc đã bị xóa."));
        }

        return Ok(ApiResponseFactory.Success(product));
    }

    /// <summary>Create a new product.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminProductDetailResponse>>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await adminProductService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            ApiResponseFactory.Success(product, "Tạo sản phẩm thành công."));
    }

    /// <summary>Update an existing product.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminProductDetailResponse>>> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await adminProductService.UpdateAsync(id, request, cancellationToken);

        if (product is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy sản phẩm.",
                "not_found",
                "Sản phẩm không tồn tại hoặc đã bị xóa."));
        }

        return Ok(ApiResponseFactory.Success(product, "Cập nhật sản phẩm thành công."));
    }

    /// <summary>Toggle product status.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(
        Guid id,
        ToggleProductStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await adminProductService.ToggleStatusAsync(id, request.Status, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy sản phẩm.",
                "not_found",
                "Sản phẩm không tồn tại hoặc đã bị xóa."));
        }

        return Ok(ApiResponseFactory.Success<object?>(null, "Cập nhật trạng thái thành công."));
    }

    /// <summary>Soft-delete a product.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminProductService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy sản phẩm.",
                "not_found",
                "Sản phẩm không tồn tại hoặc đã bị xóa."));
        }

        return NoContent();
    }

    /// <summary>Restore a soft-deleted product.</summary>
    [HttpPatch("{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminProductService.RestoreAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy sản phẩm.",
                "not_found",
                "Sản phẩm không tồn tại hoặc chưa bị xóa."));
        }

        return Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục sản phẩm thành công."));
    }
}
