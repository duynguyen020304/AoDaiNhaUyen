using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.DTOs.Marketing;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

/// <summary>Admin promo code management endpoints.</summary>
[ApiController]
[Route("api/admin/promos")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminPromosController(IAdminPromoService adminPromoService, IPromoCostService promoCostService) : ControllerBase
{
  /// <summary>Get promo codes for admin.</summary>
  [HttpGet]
  public async Task<ActionResult<PaginatedApiResponse<IReadOnlyList<AdminPromoListItemResponse>>>> GetAll(
    [FromQuery] bool includeDeleted = false,
    [FromQuery] string? search = null,
    [FromQuery] bool? isActive = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var (items, totalItem) = await adminPromoService.GetAllAdminAsync(
      includeDeleted,
      search,
      isActive,
      page,
      pageSize,
      cancellationToken);

    return Ok(ApiResponseFactory.PaginatedSuccess(items, page, pageSize, totalItem));
  }

  /// <summary>Get a single promo code by ID.</summary>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ApiResponse<AdminPromoDetailResponse>>> GetById(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var promo = await adminPromoService.GetByIdAsync(id, cancellationToken);

    if (promo is null)
    {
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy mã giảm giá.",
        "not_found",
        "Mã giảm giá không tồn tại."));
    }

    return Ok(ApiResponseFactory.Success(promo));
  }

  /// <summary>Get promo cost/revenue performance.</summary>
  [HttpGet("{id:guid}/performance")]
  public async Task<ActionResult<ApiResponse<PromoPerformanceDto>>> GetPerformance(
    Guid id,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null,
    CancellationToken cancellationToken = default)
  {
    var performance = await promoCostService.GetPromoPerformanceAsync(id, from, to, cancellationToken);
    if (performance is null)
    {
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy mã giảm giá.",
        "not_found",
        "Mã giảm giá không tồn tại."));
    }

    return Ok(ApiResponseFactory.Success(performance, "Lấy hiệu quả mã giảm giá thành công."));
  }

  /// <summary>Create a new promo code.</summary>
  [HttpPost]
  public async Task<ActionResult<ApiResponse<AdminPromoDetailResponse>>> Create(
    CreatePromoRequest request,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var promo = await adminPromoService.CreatePromoAsync(request, cancellationToken);
      return CreatedAtAction(nameof(GetById), new { id = promo.Id },
        ApiResponseFactory.Success(promo, "Tạo mã giảm giá thành công."));
    }
    catch (InvalidOperationException ex)
    {
      return Conflict(ApiResponseFactory.Failure("Không thể tạo mã giảm giá.", "conflict", ex.Message));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu mã giảm giá không hợp lệ.", "validation_error", ex.Message));
    }
  }

  /// <summary>Update an existing promo code.</summary>
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<ApiResponse<AdminPromoDetailResponse>>> Update(
    Guid id,
    UpdatePromoRequest request,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var promo = await adminPromoService.UpdateAsync(id, request, cancellationToken);

      if (promo is null)
      {
        return NotFound(ApiResponseFactory.Failure(
          "Không tìm thấy mã giảm giá.",
          "not_found",
          "Mã giảm giá không tồn tại."));
      }

      return Ok(ApiResponseFactory.Success(promo, "Cập nhật mã giảm giá thành công."));
    }
    catch (InvalidOperationException ex)
    {
      return Conflict(ApiResponseFactory.Failure("Không thể cập nhật mã giảm giá.", "conflict", ex.Message));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Dữ liệu mã giảm giá không hợp lệ.", "validation_error", ex.Message));
    }
  }

  /// <summary>Toggle promo active status.</summary>
  [HttpPatch("{id:guid}/status")]
  public async Task<ActionResult<ApiResponse<object?>>> ToggleStatus(
    Guid id,
    TogglePromoStatusRequest request,
    CancellationToken cancellationToken = default)
  {
    var success = await adminPromoService.ToggleActiveAsync(id, request.IsActive, cancellationToken);

    if (!success)
    {
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy mã giảm giá.",
        "not_found",
        "Mã giảm giá không tồn tại hoặc đã bị xóa."));
    }

    return Ok(ApiResponseFactory.Success<object?>(null, "Cập nhật trạng thái mã giảm giá thành công."));
  }

  /// <summary>Soft-delete a promo code.</summary>
  [HttpDelete("{id:guid}")]
  public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
  {
    var success = await adminPromoService.DeleteAsync(id, cancellationToken);

    if (!success)
    {
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy mã giảm giá.",
        "not_found",
        "Mã giảm giá không tồn tại hoặc đã bị xóa."));
    }

    return NoContent();
  }

  /// <summary>Restore a soft-deleted promo code.</summary>
  [HttpPatch("{id:guid}/restore")]
  public async Task<ActionResult<ApiResponse<object?>>> Restore(
    Guid id,
    CancellationToken cancellationToken = default)
  {
    var success = await adminPromoService.RestoreAsync(id, cancellationToken);

    if (!success)
    {
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy mã giảm giá.",
        "not_found",
        "Mã giảm giá không tồn tại hoặc chưa bị xóa."));
    }

    return Ok(ApiResponseFactory.Success<object?>(null, "Khôi phục mã giảm giá thành công."));
  }
}
