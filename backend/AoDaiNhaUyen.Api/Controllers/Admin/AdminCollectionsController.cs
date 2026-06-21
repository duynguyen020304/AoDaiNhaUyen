using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Collections;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/collections")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminCollectionsController(IAdminCollectionService service) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool includeDeleted = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);
    var result = await service.GetListAsync(search, includeDeleted, page, pageSize, ct);
    return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, page, pageSize, result.TotalCount));
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
  {
    var item = await service.GetByIdAsync(id, true, ct);
    return item is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy collection.", "not_found", "Collection không tồn tại."))
      : Ok(ApiResponseFactory.Success(item));
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateCollectionRequest request, CancellationToken ct = default)
  {
    try
    {
      var item = await service.CreateAsync(request, ct);
      return Ok(ApiResponseFactory.Success(item, "Tạo collection thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu collection không hợp lệ.", "validation_error", ex.Message));
    }
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, UpdateCollectionRequest request, CancellationToken ct = default)
  {
    try
    {
      var item = await service.UpdateAsync(id, request, ct);
      return item is null
        ? NotFound(ApiResponseFactory.Failure("Không tìm thấy collection.", "not_found", "Collection không tồn tại hoặc đã bị xóa."))
        : Ok(ApiResponseFactory.Success(item, "Cập nhật collection thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu collection không hợp lệ.", "validation_error", ex.Message));
    }
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
  {
    return await service.DeleteAsync(id, ct)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Xóa collection thành công."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy collection.", "not_found", "Collection không tồn tại hoặc đã bị xóa."));
  }

  [HttpPatch("{id:guid}/restore")]
  public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
  {
    return await service.RestoreAsync(id, ct)
      ? Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục collection thành công."))
      : NotFound(ApiResponseFactory.Failure("Không tìm thấy collection.", "not_found", "Collection không tồn tại hoặc chưa bị xóa."));
  }

  [HttpPost("{id:guid}/products")]
  public async Task<IActionResult> AddProduct(Guid id, AddProductToCollectionRequest request, CancellationToken ct = default)
  {
    try
    {
      var item = await service.AddProductAsync(id, request, ct);
      return item is null
        ? NotFound(ApiResponseFactory.Failure("Không tìm thấy collection.", "not_found", "Collection không tồn tại."))
        : Ok(ApiResponseFactory.Success(item, "Đã thêm sản phẩm vào collection."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu không hợp lệ.", "validation_error", ex.Message));
    }
  }

  [HttpDelete("{id:guid}/products/{productId:guid}")]
  public async Task<IActionResult> RemoveProduct(Guid id, Guid productId, CancellationToken ct = default)
  {
    var item = await service.RemoveProductAsync(id, productId, ct);
    return item is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy sản phẩm trong collection.", "not_found", "Liên kết không tồn tại."))
      : Ok(ApiResponseFactory.Success(item, "Đã xóa sản phẩm khỏi collection."));
  }
}
