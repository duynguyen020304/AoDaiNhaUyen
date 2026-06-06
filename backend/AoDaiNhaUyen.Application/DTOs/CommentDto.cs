namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CommentDto(
  Guid Id,
  Guid UserId,
  string UserFullName,
  string? UserAvatarUrl,
  string Content,
  int? Rating,
  Guid? ParentCommentId,
  DateTime CreatedAt,
  IReadOnlyList<CommentDto> Replies);
