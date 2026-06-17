namespace AoDaiNhaUyen.Application.Interfaces.Services;

/// <summary>Admin review/comment management service for AI agent.</summary>
public interface IAdminReviewService
{
  /// <summary>List recent reviews across all products.</summary>
  Task<IReadOnlyList<AdminReviewItem>> GetRecentReviewsAsync(int limit = 10, CancellationToken ct = default);

  /// <summary>List product reviews for admin moderation.</summary>
  Task<AdminReviewListResult> GetReviewsAsync(AdminReviewListQuery query, CancellationToken ct = default);

  /// <summary>Toggle review visibility.</summary>
  Task<AdminReviewActionResult> SetReviewVisibilityAsync(Guid id, bool isVisible, CancellationToken ct = default);

  /// <summary>Delete a review/comment permanently.</summary>
  Task<AdminReviewActionResult> DeleteReviewAsync(Guid id, CancellationToken ct = default);

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

public sealed record AdminReviewListQuery(
  string? Search,
  int? Rating,
  bool? IsVisible,
  int Page = 1,
  int PageSize = 20);

public sealed record AdminReviewListResult(
  IReadOnlyList<AdminReviewModerationItem> Items,
  int TotalCount);

public sealed record AdminReviewModerationItem(
  Guid Id,
  Guid UserId,
  string? UserName,
  string? UserEmail,
  Guid ProductId,
  string? ProductName,
  int Rating,
  string Content,
  bool IsVisible,
  int ReplyCount,
  DateTimeOffset CreatedAt);

public sealed record AdminReviewActionResult(
  bool Success,
  string Message);
