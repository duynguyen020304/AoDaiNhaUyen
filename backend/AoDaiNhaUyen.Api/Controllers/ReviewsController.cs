using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/reviews")]
public sealed class ReviewsController(ICommentService commentService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetReviews(
    Guid productId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
  {
    var result = await commentService.GetProductReviewsAsync(productId, page, pageSize, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(
      result.Items,
      result.Page,
      result.PageSize,
      result.TotalCount));
  }

  [HttpPost]
  [Authorize(Policy = "RequireAdminOrCustomer")]
  public async Task<IActionResult> CreateReview(
    Guid productId,
    [FromBody] CreateReviewRequest request,
    CancellationToken cancellationToken)
  {
    var userId = GetCurrentUserId();
    if (userId == Guid.Empty)
    {
      return Unauthorized(ApiResponseFactory.Failure(
        "Vui lòng đăng nhập để đánh giá.",
        "unauthorized",
        "User not authenticated."));
    }

    try
    {
      var review = await commentService.CreateReviewAsync(userId, productId, request, cancellationToken);
      return Ok(ApiResponseFactory.Success(review, "Đánh giá thành công."));
    }
    catch (ArgumentException ex)
    {
      return BadRequest(ApiResponseFactory.Failure(
        ex.Message,
        "validation_error",
        ex.Message));
    }
    catch (InvalidOperationException ex)
    {
      return Conflict(ApiResponseFactory.Failure(
        ex.Message,
        "already_reviewed",
        ex.Message));
    }
  }

  private Guid GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
  }
}
