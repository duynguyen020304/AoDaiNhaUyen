using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin category management endpoints.</summary>
[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminCategoriesController(IAdminCategoryService adminCategoryService) : ControllerBase
{
    /// <summary>Get all categories (flat list).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminCategoryListItemResponse>>>> GetAll(
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await adminCategoryService.GetAllAsync(includeDeleted, cancellationToken);
        return Ok(ApiResponseFactory.Success(categories));
    }

    /// <summary>Get a single category by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminCategoryDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await adminCategoryService.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy danh mục.",
                "not_found",
                "Danh mục không tồn tại."));
        }

        return Ok(ApiResponseFactory.Success(category));
    }

    /// <summary>Create a new category.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminCategoryDetailResponse>>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await adminCategoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            ApiResponseFactory.Success(category, "Tạo danh mục thành công."));
    }

    /// <summary>Update an existing category.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminCategoryDetailResponse>>> Update(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await adminCategoryService.UpdateAsync(id, request, cancellationToken);

        if (category is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy danh mục.",
                "not_found",
                "Danh mục không tồn tại."));
        }

        return Ok(ApiResponseFactory.Success(category, "Cập nhật danh mục thành công."));
    }

    /// <summary>Soft-delete a category.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminCategoryService.DeleteAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy danh mục.",
                "not_found",
                "Danh mục không tồn tại hoặc đã bị xóa."));
        }

        return NoContent();
    }

    /// <summary>Restore a soft-deleted category.</summary>
    [HttpPatch("{id:guid}/restore")]
    public async Task<ActionResult<ApiResponse<object?>>> Restore(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var success = await adminCategoryService.RestoreAsync(id, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy danh mục.",
                "not_found",
                "Danh mục không tồn tại hoặc chưa bị xóa."));
        }

        return Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục danh mục thành công."));
    }
}
