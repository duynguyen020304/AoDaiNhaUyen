using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin product management endpoints.</summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminProductsController(
  IAdminProductService adminProductService,
  IImageVisibilityService imageVisibilityService,
  ICacheInvalidationService cacheInvalidation) : ControllerBase
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

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);

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

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success(product, "Cập nhật sản phẩm thành công."));
    }

    /// <summary>Update stock quantity for a product variant.</summary>
    [HttpPatch("{productId:guid}/variants/{variantId:guid}/stock")]
    public async Task<ActionResult<ApiResponse<AdminProductDetailResponse>>> UpdateVariantStock(
        Guid productId,
        Guid variantId,
        UpdateVariantStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = await adminProductService.UpdateVariantStockAsync(productId, variantId, request.StockQty, cancellationToken);

        if (product is null)
        {
            return NotFound(ApiResponseFactory.Failure(
                "Không tìm thấy biến thể sản phẩm.",
                "not_found",
                "Sản phẩm hoặc biến thể không tồn tại."));
        }

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success(product, "Cập nhật tồn kho thành công."));
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

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
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

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
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

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục sản phẩm thành công."));
    }

    /// <summary>Promote a product image to public (accessible via direct URL).</summary>
    [HttpPost("{productId:guid}/images/{imageId:guid}/make-public")]
    public async Task<ActionResult<ApiResponse<ProductImageVisibilityDto>>> MakeImagePublic(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var result = await imageVisibilityService.MakePublicAsync(imageId, productId, cancellationToken);
        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success(result, "Chuyển ảnh sang công khai thành công."));
    }

    /// <summary>Demote a product image to private (accessible via presigned URL only).</summary>
    [HttpPost("{productId:guid}/images/{imageId:guid}/make-private")]
    public async Task<ActionResult<ApiResponse<ProductImageVisibilityDto>>> MakeImagePrivate(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var product = await adminProductService.GetByIdAsync(productId, cancellationToken);
        if (product != null && product.Status == "active")
        {
            return BadRequest(ApiResponseFactory.Failure("Không thể ẩn ảnh khi sản phẩm đang ở trạng thái 'Đăng bán'.", "bad_request", "Lỗi nghiệp vụ"));
        }

        var result = await imageVisibilityService.MakePrivateAsync(imageId, productId, cancellationToken);
        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success(result, "Chuyển ảnh sang riêng tư thành công."));
    }    /// <summary>Upload a new image for a product.</summary>
    [HttpPost("{productId:guid}/images")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<AdminImageResponse>>> UploadImage(
        Guid productId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponseFactory.Failure("File không hợp lệ.", "bad_request", "Vui lòng chọn một file ảnh hợp lệ."));
        }

        using var stream = file.OpenReadStream();
        var result = await adminProductService.UploadImageAsync(productId, stream, file.FileName, file.ContentType, cancellationToken);
        
        if (result is null)
        {
            return NotFound(ApiResponseFactory.Failure("Không tìm thấy sản phẩm.", "not_found", "Sản phẩm không tồn tại hoặc đã bị xóa."));
        }

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success(result, "Tải ảnh lên thành công."));
    }

    /// <summary>Delete a product image.</summary>
    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var success = await adminProductService.DeleteImageAsync(productId, imageId, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure("Không tìm thấy ảnh hoặc sản phẩm.", "not_found", "Ảnh hoặc sản phẩm không tồn tại."));
        }

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success<object?>(null, "Xóa ảnh thành công."));
    }

    /// <summary>Set a product image as primary.</summary>
    [HttpPut("{productId:guid}/images/{imageId:guid}/primary")]
    public async Task<ActionResult<ApiResponse<object>>> SetPrimaryImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var success = await adminProductService.SetPrimaryImageAsync(productId, imageId, cancellationToken);
        if (!success)
        {
            return NotFound(ApiResponseFactory.Failure("Không tìm thấy ảnh hoặc sản phẩm.", "not_found", "Ảnh hoặc sản phẩm không tồn tại."));
        }

        await cacheInvalidation.InvalidateProductRelatedCacheAsync(CancellationToken.None);
        return Ok(ApiResponseFactory.Success<object?>(null, "Cập nhật ảnh chính thành công."));
    }
}
