using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminReviewService(
  AppDbContext dbContext,
  IHermesEventOutboxPublisher hermesEvents) : IAdminReviewService
{
  public async Task<IReadOnlyList<AdminReviewItem>> GetRecentReviewsAsync(
    int limit = 10, CancellationToken ct = default)
  {
    var reviews = await dbContext.Comments
      .AsNoTracking()
      .Where(c => c.Rating.HasValue && c.ParentCommentId == null && c.IsVisible)
      .OrderByDescending(c => c.CreatedAt)
      .Take(Math.Clamp(limit, 1, 50))
      .Select(c => new AdminReviewItem(
        c.Id,
        c.UserId,
        c.User.FullName,
        c.ProductId,
        c.Product.Name,
        c.Rating!.Value,
        c.Content,
        c.CreatedAt))
      .ToListAsync(ct);

    return reviews;
  }

  public async Task<AdminReviewListResult> GetReviewsAsync(
    AdminReviewListQuery query,
    CancellationToken ct = default)
  {
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

    var reviewsQuery = dbContext.Comments
      .AsNoTracking()
      .Include(c => c.User)
      .Include(c => c.Product)
      .Where(c => c.Rating.HasValue && c.ParentCommentId == null);

    if (query.Rating.HasValue)
    {
      reviewsQuery = reviewsQuery.Where(c => c.Rating == query.Rating.Value);
    }

    if (query.IsVisible.HasValue)
    {
      reviewsQuery = reviewsQuery.Where(c => c.IsVisible == query.IsVisible.Value);
    }

    if (!string.IsNullOrWhiteSpace(query.Search))
    {
      var term = query.Search.Trim().ToLower();
      reviewsQuery = reviewsQuery.Where(c =>
        c.Content.ToLower().Contains(term)
        || c.User.FullName.ToLower().Contains(term)
        || (c.User.Email != null && c.User.Email.ToLower().Contains(term))
        || c.Product.Name.ToLower().Contains(term));
    }

    var totalCount = await reviewsQuery.CountAsync(ct);
    var items = await reviewsQuery
      .OrderByDescending(c => c.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(c => new AdminReviewModerationItem(
        c.Id,
        c.UserId,
        c.User.FullName,
        c.User.Email,
        c.ProductId,
        c.Product.Name,
        c.Rating!.Value,
        c.Content,
        c.IsVisible,
        c.Replies.Count(r => r.IsVisible),
        c.CreatedAt))
      .ToListAsync(ct);

    return new AdminReviewListResult(items, totalCount);
  }

  public async Task<AdminReviewActionResult> SetReviewVisibilityAsync(
    Guid id, bool isVisible, CancellationToken ct = default)
  {
    var review = await dbContext.Comments
      .Include(c => c.User)
      .Include(c => c.Product)
      .FirstOrDefaultAsync(c => c.Id == id && c.Rating.HasValue && c.ParentCommentId == null, ct);

    if (review is null)
      return new AdminReviewActionResult(false, "Không tìm thấy đánh giá.");

    review.IsVisible = isVisible;
    await dbContext.SaveChangesAsync(ct);
    await EnqueueReviewModerationEventAsync(review, isVisible ? "shown" : "hidden", ct);

    return new AdminReviewActionResult(true, isVisible ? "Đã hiển thị đánh giá." : "Đã ẩn đánh giá.");
  }

  public async Task<AdminReviewActionResult> DeleteReviewAsync(Guid id, CancellationToken ct = default)
  {
    var review = await dbContext.Comments
      .Include(c => c.User)
      .Include(c => c.Product)
      .FirstOrDefaultAsync(c => c.Id == id && c.Rating.HasValue && c.ParentCommentId == null, ct);

    if (review is null)
      return new AdminReviewActionResult(false, "Không tìm thấy đánh giá.");

    await EnqueueReviewModerationEventAsync(review, "deleted", ct);
    dbContext.Comments.Remove(review);
    await dbContext.SaveChangesAsync(ct);

    return new AdminReviewActionResult(true, "Đã xóa đánh giá.");
  }

  public async Task<IReadOnlyList<AdminCommentItem>> GetRecentCommentsAsync(
    int limit = 10, CancellationToken ct = default)
  {
    var comments = await dbContext.Comments
      .AsNoTracking()
      .Where(c => c.IsVisible)
      .OrderByDescending(c => c.CreatedAt)
      .Take(Math.Clamp(limit, 1, 50))
      .Select(c => new AdminCommentItem(
        c.Id,
        c.UserId,
        c.User.FullName,
        c.ProductId,
        c.Product.Name,
        c.Content,
        c.ParentCommentId,
        c.CreatedAt))
      .ToListAsync(ct);

    return comments;
  }

  public async Task<BadReviewRecoveryStats> GetBadReviewRecoveryStatsAsync(
    int days = 30, double slaHours = 4, CancellationToken ct = default)
  {
    var normalizedDays = Math.Clamp(days, 1, 365);
    var normalizedSlaHours = slaHours is > 0 and <= 168 ? slaHours : 4;
    var cutoff = DateTime.UtcNow.AddDays(-normalizedDays);
    var now = DateTime.UtcNow;

    var badReviews = await dbContext.Comments
      .AsNoTracking()
      .Where(c => c.Rating.HasValue
        && c.Rating.Value <= 2
        && c.ParentCommentId == null
        && !c.IsDeleted
        && c.CreatedAt >= cutoff)
      .Select(c => new
      {
        c.Id,
        c.CreatedAt,
        FirstReplyAt = c.Replies
          .Where(r => r.IsVisible && !r.IsDeleted)
          .OrderBy(r => r.CreatedAt)
          .Select(r => (DateTime?)r.CreatedAt)
          .FirstOrDefault()
      })
      .ToListAsync(ct);

    var totalBadReviews = badReviews.Count;
    var respondedBadReviews = badReviews.Count(x => x.FirstReplyAt.HasValue);
    var unrespondedBadReviews = totalBadReviews - respondedBadReviews;
    var overSlaBadReviews = badReviews.Count(x =>
    {
      var end = x.FirstReplyAt ?? now;
      return (end - x.CreatedAt).TotalHours > normalizedSlaHours;
    });

    var recoveryActionRate = totalBadReviews == 0 ? 100d : (double)respondedBadReviews / totalBadReviews * 100d;
    var slaBreachRate = totalBadReviews == 0 ? 0d : (double)overSlaBadReviews / totalBadReviews * 100d;
    var averageFirstResponseHours = respondedBadReviews == 0
      ? 0d
      : badReviews
        .Where(x => x.FirstReplyAt.HasValue)
        .Average(x => (x.FirstReplyAt!.Value - x.CreatedAt).TotalHours);

    return new BadReviewRecoveryStats(
      normalizedDays,
      normalizedSlaHours,
      totalBadReviews,
      respondedBadReviews,
      unrespondedBadReviews,
      overSlaBadReviews,
      Math.Round(recoveryActionRate, 2),
      Math.Round(slaBreachRate, 2),
      Math.Round(averageFirstResponseHours, 2));
  }

  private async Task EnqueueReviewModerationEventAsync(Comment review, string action, CancellationToken ct)
  {
    await hermesEvents.EnqueueAdminEventAsync(
      "review_moderation_changed",
      "Review",
      review.Id.ToString("N"),
      new
      {
        reviewId = review.Id,
        productId = review.ProductId,
        productName = review.Product?.Name,
        rating = review.Rating,
        content = Truncate(review.Content, 500),
        customerName = review.User?.FullName,
        isVisible = review.IsVisible,
        action,
        moderatedAt = DateTimeOffset.UtcNow
      },
      $"review_moderation_changed:{review.Id:N}:{action}:{DateTime.UtcNow.Ticks}",
      review.ProductId.ToString("N"),
      ct);
  }

  private static string Truncate(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..Math.Max(0, maxLength - 1)] + "…";
  }

  public async Task<AdminReplyResult> ReplyToCommentAsync(
    Guid adminUserId, Guid commentId, Guid productId, string content, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(content))
      return new AdminReplyResult(false, "Nội dung phản hồi không được để trống.", null);

    var parentComment = await dbContext.Comments.FindAsync([commentId], ct);
    if (parentComment is null)
      return new AdminReplyResult(false, "Không tìm thấy bình luận gốc.", null);

    var reply = new Comment
    {
      UserId = adminUserId,
      ProductId = productId,
      ParentCommentId = commentId,
      Content = content.Trim(),
      IsVisible = true
    };

    dbContext.Comments.Add(reply);
    await dbContext.SaveChangesAsync(ct);

    if (parentComment.Rating is <= 2)
    {
      await hermesEvents.EnqueueAdminEventAsync(
        "review_recovery_initiated",
        "Review",
        parentComment.Id.ToString("N"),
        new
        {
          reviewId = parentComment.Id,
          productId = parentComment.ProductId,
          rating = parentComment.Rating,
          content = Truncate(parentComment.Content, 500),
          replyId = reply.Id,
          hasAdminReply = true,
          recoveredAt = DateTimeOffset.UtcNow
        },
        $"review_recovery_initiated:Review:{parentComment.Id:N}:{reply.Id:N}",
        parentComment.ProductId.ToString("N"),
        ct);
    }

    return new AdminReplyResult(true, $"Đã phản hồi bình luận. (ID: {reply.Id})", reply.Id);
  }
}
