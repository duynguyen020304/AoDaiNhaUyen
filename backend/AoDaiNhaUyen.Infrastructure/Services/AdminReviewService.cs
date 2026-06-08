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
