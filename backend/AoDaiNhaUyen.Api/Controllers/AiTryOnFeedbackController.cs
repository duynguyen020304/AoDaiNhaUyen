using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AoDaiNhaUyen.Api.Responses;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AoDaiNhaUyen.Api.Controllers;

[ApiController]
[Route("api/v1/ai-tryon/feedback")]
public sealed class AiTryOnFeedbackController(
  IAiTryOnFeedbackService feedbackService,
  ILogger<AiTryOnFeedbackController> logger) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Create(
    [FromBody] CreateAiTryOnFeedbackDto request,
    CancellationToken cancellationToken)
  {
    if (request.GeneratedImageId == Guid.Empty)
    {
      return BadRequest(ApiResponseFactory.Failure(
        "Đánh giá không hợp lệ",
        "invalid_generated_image_id",
        "Thiếu mã ảnh AI try-on."));
    }

    try
    {
      var userId = GetCurrentUserId();
      var guestKeyHash = userId is null ? ComputeGuestKeyHash(HttpContext) : null;
      var result = await feedbackService.CreateAsync(userId, guestKeyHash, request, cancellationToken);
      return Ok(ApiResponseFactory.Success(result, "Cảm ơn bạn đã đánh giá ảnh thử đồ."));
    }
    catch (ArgumentOutOfRangeException ex)
    {
      return BadRequest(ApiResponseFactory.Failure("Đánh giá không hợp lệ", "invalid_rating", ex.Message));
    }
    catch (InvalidOperationException ex)
    {
      return NotFound(ApiResponseFactory.Failure("Không tìm thấy ảnh thử đồ", "tryon_image_not_found", ex.Message));
    }
    catch (UnauthorizedAccessException ex)
    {
      return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure("Không có quyền đánh giá", "forbidden", ex.Message));
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.ForeignKeyViolation)
    {
      logger.LogWarning(ex, "AI try-on feedback rejected by FK. GeneratedImageId={GeneratedImageId}", request.GeneratedImageId);
      return NotFound(ApiResponseFactory.Failure(
        "Không tìm thấy ảnh thử đồ",
        "tryon_image_not_found",
        "Ảnh AI try-on không tồn tại hoặc đã bị xóa."));
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UndefinedTable)
    {
      logger.LogError(ex, "AI try-on feedback table is missing. Migration AddAiTryOnFeedback has not been applied.");
      return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponseFactory.Failure(
        "Chưa sẵn sàng lưu đánh giá",
        "feedback_migration_missing",
        "Cơ sở dữ liệu chưa cập nhật bảng đánh giá AI try-on. Hãy chạy migration mới nhất."));
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
    {
      logger.LogError(ex, "AI try-on feedback table is missing. Migration AddAiTryOnFeedback has not been applied.");
      return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponseFactory.Failure(
        "Chưa sẵn sàng lưu đánh giá",
        "feedback_migration_missing",
        "Cơ sở dữ liệu chưa cập nhật bảng đánh giá AI try-on. Hãy chạy migration mới nhất."));
    }
    catch (DbUpdateException ex)
    {
      logger.LogError(ex, "Failed to save AI try-on feedback. GeneratedImageId={GeneratedImageId}", request.GeneratedImageId);
      return BadRequest(ApiResponseFactory.Failure(
        "Lưu đánh giá thất bại",
        "feedback_save_failed",
        "Không thể lưu đánh giá lúc này. Vui lòng thử lại."));
    }
  }

  private Guid? GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
  }

  private static string? ComputeGuestKeyHash(HttpContext context)
  {
    var guestKey = context.Request.Headers["X-Guest-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(guestKey)) return null;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(guestKey.Trim()));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}
