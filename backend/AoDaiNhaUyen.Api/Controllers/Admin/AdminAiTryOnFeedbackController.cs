using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/ai-tryon-feedback")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminAiTryOnFeedbackController(IAiTryOnFeedbackService feedbackService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] int? rating = null,
    [FromQuery] bool? isResolved = null,
    CancellationToken cancellationToken = default)
  {
    var result = await feedbackService.GetForAdminAsync(page, pageSize, rating, isResolved, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, result.Page, result.PageSize, result.TotalCount));
  }

  [HttpPatch("{id:guid}/status")]
  public async Task<IActionResult> UpdateStatus(
    Guid id,
    [FromBody] UpdateAiTryOnFeedbackStatusDto request,
    CancellationToken cancellationToken = default)
  {
    var result = await feedbackService.UpdateStatusAsync(id, request, cancellationToken);
    return result is null
      ? NotFound(ApiResponseFactory.Failure("Không tìm thấy đánh giá AI try-on", "not_found", "Đánh giá không tồn tại."))
      : Ok(ApiResponseFactory.Success(result, "Cập nhật trạng thái đánh giá thành công."));
  }
}
