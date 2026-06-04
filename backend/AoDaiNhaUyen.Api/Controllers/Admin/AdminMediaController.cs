using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/media")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminMediaController(IAdminMediaService adminMediaService) : ControllerBase
{
  /// <summary>
  /// Lấy tất cả ảnh (phân trang, lọc theo source/search).
  /// </summary>
  [HttpGet]
  public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? sourceType = null,
    [FromQuery] string? search = null,
    CancellationToken cancellationToken = default)
  {
    var result = await adminMediaService.GetAllAsync(page, pageSize, sourceType, search, cancellationToken);
    return Ok(ApiResponseFactory.Success(result));
  }

  /// <summary>
  /// Lấy chi tiết ảnh theo ID.
  /// </summary>
  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
  {
    var image = await adminMediaService.GetByIdAsync(id, cancellationToken);
    if (image is null)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy ảnh", "not_found", "Ảnh không tồn tại."));
    }

    return Ok(ApiResponseFactory.Success(image));
  }

  /// <summary>
  /// Xóa ảnh (soft-delete + xóa trên S3).
  /// </summary>
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
  {
    var deleted = await adminMediaService.DeleteAsync(id, cancellationToken);
    if (!deleted)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy ảnh", "not_found", "Ảnh không tồn tại."));
    }

    return Ok(ApiResponseFactory.Success(true, "Đã xóa ảnh thành công"));
  }

  /// <summary>
  /// Thống kê ảnh.
  /// </summary>
  [HttpGet("stats")]
  public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
  {
    var stats = await adminMediaService.GetStatsAsync(cancellationToken);
    return Ok(ApiResponseFactory.Success(stats));
  }
}
