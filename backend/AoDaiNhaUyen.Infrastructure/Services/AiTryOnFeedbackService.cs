using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AiTryOnFeedbackService(
  AppDbContext dbContext,
  IStorageService storageService) : IAiTryOnFeedbackService
{
  public async Task<AiTryOnFeedbackDto> CreateAsync(
    Guid? userId,
    string? guestKeyHash,
    CreateAiTryOnFeedbackDto request,
    CancellationToken cancellationToken = default)
  {
    if (request.Rating is < 1 or > 5)
      throw new ArgumentOutOfRangeException(nameof(request.Rating), "Điểm đánh giá phải từ 1 đến 5 sao.");

    var image = await dbContext.UserGeneratedImages
      .AsNoTracking()
      .FirstOrDefaultAsync(item => item.Id == request.GeneratedImageId && item.SourceType == "ai_tryon", cancellationToken);

    if (image is null)
      throw new InvalidOperationException("Không tìm thấy ảnh AI try-on để đánh giá.");

    var ownsImage = userId.HasValue
      ? image.UserId == userId.Value
      : !string.IsNullOrWhiteSpace(guestKeyHash) && image.GuestKeyHash == guestKeyHash;

    if (!ownsImage)
      throw new UnauthorizedAccessException("Bạn không có quyền đánh giá ảnh này.");

    var feedback = new AiTryOnFeedback
    {
      UserGeneratedImageId = image.Id,
      UserId = userId,
      GuestKeyHash = guestKeyHash,
      Rating = request.Rating,
      Comment = NormalizeText(request.Comment, 1000),
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    dbContext.AiTryOnFeedbacks.Add(feedback);
    await dbContext.SaveChangesAsync(cancellationToken);

    return new AiTryOnFeedbackDto(feedback.Id, feedback.UserGeneratedImageId, feedback.Rating, feedback.Comment, feedback.CreatedAt);
  }

  public async Task<PagedResult<AdminAiTryOnFeedbackDto>> GetForAdminAsync(
    int page,
    int pageSize,
    int? rating,
    bool? isResolved,
    CancellationToken cancellationToken = default)
  {
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = dbContext.AiTryOnFeedbacks
      .AsNoTracking()
      .Include(item => item.User)
      .Include(item => item.UserGeneratedImage)
      .Where(item => !item.IsDeleted);

    if (rating.HasValue)
      query = query.Where(item => item.Rating == rating.Value);

    if (isResolved.HasValue)
      query = query.Where(item => item.IsResolved == isResolved.Value);

    var total = await query.CountAsync(cancellationToken);
    var rows = await query
      .OrderByDescending(item => item.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToListAsync(cancellationToken);

    var items = new List<AdminAiTryOnFeedbackDto>(rows.Count);
    foreach (var item in rows)
    {
      var imageUrl = await ResolveImageUrlAsync(item.UserGeneratedImage, cancellationToken);
      items.Add(MapAdmin(item, imageUrl));
    }

    return new PagedResult<AdminAiTryOnFeedbackDto>(items, total, page, pageSize);
  }

  public async Task<AdminAiTryOnFeedbackDto?> UpdateStatusAsync(
    Guid id,
    UpdateAiTryOnFeedbackStatusDto request,
    CancellationToken cancellationToken = default)
  {
    var feedback = await dbContext.AiTryOnFeedbacks
      .Include(item => item.User)
      .Include(item => item.UserGeneratedImage)
      .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

    if (feedback is null) return null;

    feedback.IsResolved = request.IsResolved;
    feedback.AdminNote = NormalizeText(request.AdminNote, 1000);
    feedback.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);

    var imageUrl = await ResolveImageUrlAsync(feedback.UserGeneratedImage, cancellationToken);
    return MapAdmin(feedback, imageUrl);
  }

  private async Task<string> ResolveImageUrlAsync(UserGeneratedImage image, CancellationToken cancellationToken)
  {
    if (!string.IsNullOrWhiteSpace(image.ObjectKey))
    {
      return await storageService.GeneratePresignedGetUrlAsync(image.ObjectKey, 3600, cancellationToken);
    }

    return image.Url;
  }

  private static AdminAiTryOnFeedbackDto MapAdmin(AiTryOnFeedback item, string imageUrl) => new(
    item.Id,
    item.UserGeneratedImageId,
    imageUrl,
    item.UserId,
    item.User?.FullName,
    item.User?.Email,
    item.Rating,
    item.Comment,
    item.AdminNote,
    item.IsResolved,
    item.CreatedAt);

  private static string? NormalizeText(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }
}
