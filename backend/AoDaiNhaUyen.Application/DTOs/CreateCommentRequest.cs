namespace AoDaiNhaUyen.Application.DTOs;

public sealed record CreateCommentRequest(string Content, int? Rating = null, Guid? ParentCommentId = null);
