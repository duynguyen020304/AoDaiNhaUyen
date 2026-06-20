using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Policy = "RequireAdminRole")]
public sealed class AdminReviewsController(IAdminReviewService reviewService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetReviews(
    [FromQuery] string? search,
    [FromQuery] int? rating,
    [FromQuery] bool? isVisible,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
  {
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var result = await reviewService.GetReviewsAsync(
      new AdminReviewListQuery(search, rating, isVisible, page, pageSize),
      cancellationToken);

    return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, page, pageSize, result.TotalCount));
  }

  [HttpGet("recovery-stats")]
  public async Task<IActionResult> GetRecoveryStats(
    [FromQuery] int days = 30,
    [FromQuery] double slaHours = 4,
    CancellationToken cancellationToken = default)
  {
    var stats = await reviewService.GetBadReviewRecoveryStatsAsync(days, slaHours, cancellationToken);
    return Ok(ApiResponseFactory.Success(stats, "Lấy thống kê chăm sóc đánh giá xấu thành công."));
  }

  [HttpPatch("{id:guid}/visibility")]
  public async Task<IActionResult> SetVisibility(
    Guid id,
    [FromBody] SetReviewVisibilityRequest request,
    CancellationToken cancellationToken = default)
  {
    var result = await reviewService.SetReviewVisibilityAsync(id, request.IsVisible, cancellationToken);
    if (!result.Success)
    {
      return NotFound(ApiResponseFactory.Failure(result.Message, "not_found", result.Message));
    }

    return Ok(ApiResponseFactory.Success(result, result.Message));
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
  {
    var result = await reviewService.DeleteReviewAsync(id, cancellationToken);
    if (!result.Success)
    {
      return NotFound(ApiResponseFactory.Failure(result.Message, "not_found", result.Message));
    }

    return Ok(ApiResponseFactory.Success(result, result.Message));
  }

  [HttpPost("{id:guid}/reply")]
  public async Task<IActionResult> Reply(
    Guid id,
    [FromBody] ReplyToReviewRequest request,
    CancellationToken cancellationToken = default)
  {
    var adminUserId = GetCurrentUserId();
    if (adminUserId == Guid.Empty)
    {
      return Unauthorized(ApiResponseFactory.Failure(
        "Vui lòng đăng nhập lại.",
        "unauthorized",
        "Không xác định được tài khoản quản trị."));
    }

    var result = await reviewService.ReplyToCommentAsync(
      adminUserId,
      id,
      request.ProductId,
      request.Content,
      cancellationToken);

    if (!result.Success)
    {
      return BadRequest(ApiResponseFactory.Failure(result.Message, "reply_failed", result.Message));
    }

    return Ok(ApiResponseFactory.Success(result, result.Message));
  }

  private Guid GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
  }
}

public sealed record SetReviewVisibilityRequest(bool IsVisible);
public sealed record ReplyToReviewRequest(Guid ProductId, string Content);
