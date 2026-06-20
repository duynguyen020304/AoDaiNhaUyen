namespace AoDaiNhaUyen.Application.DTOs.Facebook;

public sealed record FacebookConversationListDto(
  IReadOnlyList<FacebookConversationDto> Items,
  string? BeforeCursor,
  string? AfterCursor,
  string? NextUrl);

public sealed record FacebookConversationDto(
  string Id,
  string PageId,
  string? CustomerId,
  string? CustomerName,
  string? CustomerAvatarUrl,
  string? Snippet,
  DateTimeOffset? UpdatedTime,
  int? UnreadCount,
  int? MessageCount,
  string? Link,
  IReadOnlyList<FacebookParticipantDto> Participants);

public sealed record FacebookParticipantDto(
  string? Id,
  string? Name,
  string? Email,
  bool IsPage);

public sealed record FacebookMessageListDto(
  IReadOnlyList<FacebookMessageDto> Items,
  string? BeforeCursor,
  string? AfterCursor,
  string? NextUrl);

public sealed record FacebookMessageDto(
  string Id,
  string ConversationId,
  string? SenderId,
  string? SenderName,
  bool IsFromPage,
  string? Text,
  DateTimeOffset? CreatedTime,
  IReadOnlyList<FacebookMessageAttachmentDto> Attachments);

public sealed record FacebookMessageAttachmentDto(
  string? Type,
  string? Url,
  string? Name,
  string? MimeType,
  long? Size);

public sealed record SendFacebookMessageRequest(
  string? Text,
  string? AttachmentUrl,
  string? AttachmentType);

public sealed record FacebookMessageSendResultDto(
  bool Success,
  string? MessageId);

public sealed record MarkConversationReadResultDto(bool Success);
