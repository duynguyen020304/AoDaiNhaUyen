namespace AoDaiNhaUyen.Application.DTOs.Facebook;

public sealed record FacebookPostCommentListDto(
  IReadOnlyList<FacebookCommentDto> Items,
  string? BeforeCursor,
  string? AfterCursor,
  string? NextUrl);

public sealed record FacebookCommentDto(
  string Id,
  string? PostId,
  string? ParentId,
  FacebookCommentAuthorDto? Author,
  string? Message,
  DateTimeOffset? CreatedTime,
  int? LikeCount,
  int? CommentCount,
  bool? CanReply,
  bool? CanHide,
  bool? CanDelete,
  bool? IsHidden,
  IReadOnlyList<FacebookCommentDto> Replies);

public sealed record FacebookCommentAuthorDto(
  string? Id,
  string? Name,
  string? AvatarUrl);

public sealed record CreateFacebookCommentRequest(string Message);

public sealed record ReplyFacebookCommentRequest(string Message);

public sealed record ToggleFacebookCommentHiddenRequest(bool IsHidden);

public sealed record FacebookCommentActionResultDto(
  bool Success,
  string? Id,
  string? Message);
