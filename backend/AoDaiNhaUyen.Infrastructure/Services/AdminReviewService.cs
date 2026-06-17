using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class AdminReviewService(AppDbContext dbContext) : IAdminReviewService
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
      .FirstOrDefaultAsync(c => c.Id == id && c.Rating.HasValue && c.ParentCommentId == null, ct);

    if (review is null)
      return new AdminReviewActionResult(false, "Không tìm thấy đánh giá.");

    review.IsVisible = isVisible;
    await dbContext.SaveChangesAsync(ct);

    return new AdminReviewActionResult(true, isVisible ? "Đã hiển thị đánh giá." : "Đã ẩn đánh giá.");
  }

  public async Task<AdminReviewActionResult> DeleteReviewAsync(Guid id, CancellationToken ct = default)
  {
    var review = await dbContext.Comments
      .FirstOrDefaultAsync(c => c.Id == id && c.Rating.HasValue && c.ParentCommentId == null, ct);

    if (review is null)
      return new AdminReviewActionResult(false, "Không tìm thấy đánh giá.");

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

    return new AdminReplyResult(true, $"Đã phản hồi bình luận. (ID: {reply.Id})", reply.Id);
  }
}
