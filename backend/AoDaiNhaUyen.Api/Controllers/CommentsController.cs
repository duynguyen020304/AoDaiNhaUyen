using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/comments")]
public sealed class CommentsController(ICommentService commentService) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetComments(
    Guid productId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    CancellationToken cancellationToken = default)
  {
    var result = await commentService.GetProductCommentsAsync(productId, page, pageSize, cancellationToken);
    return Ok(ApiResponseFactory.PaginatedSuccess(
      result.Items,
      result.Page,
      result.PageSize,
      result.TotalCount));
  }

  [HttpPost]
  [Authorize(Policy = "RequireAdminOrCustomer")]
  public async Task<IActionResult> CreateComment(
    Guid productId,
    [FromBody] CreateCommentRequest request,
    CancellationToken cancellationToken)
  {
    var userId = GetCurrentUserId();
    if (userId == Guid.Empty)
    {
      return Unauthorized(ApiResponseFactory.Failure(
        "Vui lòng đăng nhập để bình luận.",
        "unauthorized",
        "User not authenticated."));
    }

    try
    {
      var comment = await commentService.CreateCommentAsync(userId, productId, request, cancellationToken);
      return Ok(ApiResponseFactory.Success(comment, "Bình luận thành công."));
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
        "already_rated",
        ex.Message));
    }
  }

  private Guid GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
  }
}
