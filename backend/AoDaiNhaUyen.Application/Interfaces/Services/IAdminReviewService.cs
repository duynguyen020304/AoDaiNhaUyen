namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin review/comment management service for AI agent.</summary>
public interface IAdminReviewService
{
  /// <summary>List recent reviews across all products.</summary>
  Task<IReadOnlyList<AdminReviewItem>> GetRecentReviewsAsync(int limit = 10, CancellationToken ct = default);

  /// <summary>List recent comments across all products.</summary>
  Task<IReadOnlyList<AdminCommentItem>> GetRecentCommentsAsync(int limit = 10, CancellationToken ct = default);

  /// <summary>Reply to a comment/review as admin (creates child comment).</summary>
  Task<AdminReplyResult> ReplyToCommentAsync(
    Guid adminUserId, Guid commentId, Guid productId, string content, CancellationToken ct = default);
}

public sealed record AdminReviewItem(
  Guid Id,
  Guid UserId,
  string? UserName,
  Guid ProductId,
  string? ProductName,
  int Rating,
  string Content,
  DateTimeOffset CreatedAt);

public sealed record AdminCommentItem(
  Guid Id,
  Guid UserId,
  string? UserName,
  Guid ProductId,
  string? ProductName,
  string Content,
  Guid? ParentCommentId,
  DateTimeOffset CreatedAt);

public sealed record AdminReplyResult(
  bool Success,
  string Message,
  Guid? CommentId);
