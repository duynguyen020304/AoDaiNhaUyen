namespace AoDaiNhaUyen.Application.DTOs.Social;

public sealed record SocialAccountConnectionDto(
  Guid Id,
  string Provider,
  string Platform,
  string ZernioProfileId,
  string ZernioAccountId,
  string? DisplayName,
  string? Username,
  string? AvatarUrl,
  DateTimeOffset? LastSyncedAt,
  bool IsActive);

public sealed record SocialConnectUrlDto(
  string AuthUrl,
  string? State);

public sealed record CreateSocialConnectUrlRequest(
  string Platform,
  string ProfileId,
  string RedirectUrl,
  bool Headless = false);

public sealed record SelectFacebookPageRequest(
  string ProfileId,
  string PageId,
  string TempToken,
  ZernioUserProfileDto UserProfile,
  string RedirectUrl);

public sealed record ZernioUserProfileDto(
  string Id,
  string Name,
  string ProfilePicture);

public sealed record CreateSocialPostRequest(
  string Content,
  IReadOnlyList<Guid> AccountIds,
  bool PublishNow,
  DateTimeOffset? ScheduledFor,
  IReadOnlyList<string>? MediaUrls);

public sealed record SocialPostPlatformDto(
  string Platform,
  string AccountId,
  string? Status,
  string? PlatformPostId,
  DateTimeOffset? PublishedAt,
  string? PlatformPostUrl,
  string? ErrorMessage);

public sealed record SocialPostMediaDto(
  string? Type,
  string Url);

public sealed record SocialPostDto(
  string Id,
  string? Content,
  string? Status,
  DateTimeOffset? ScheduledFor,
  DateTimeOffset? PublishedAt,
  string? PlatformPostUrl,
  IReadOnlyList<SocialPostPlatformDto> Platforms,
  IReadOnlyList<SocialPostMediaDto> MediaItems);

public sealed record UpdateSocialPostRequest(
  string? Content,
  bool? PublishNow,
  DateTimeOffset? ScheduledFor,
  IReadOnlyList<Guid>? AccountIds,
  IReadOnlyList<string>? MediaUrls);

public sealed record UnpublishSocialPostRequest(
  string Platform);

public sealed record SocialPostActionResultDto(
  bool Success,
  string? Message);

public sealed record SocialPostListDto(
  IReadOnlyList<SocialPostDto> Items,
  int Page,
  int Limit);

public sealed record SocialAnalyticsMetricsDto(
  long Impressions,
  long Likes,
  long Comments,
  long Shares,
  long Clicks,
  long Views);

public sealed record SocialAnalyticsDto(
  string Platform,
  DateOnly FromDate,
  DateOnly ToDate,
  SocialAnalyticsMetricsDto Posts);

public sealed record SocialCommentedPostListDto(
  IReadOnlyList<SocialCommentedPostDto> Items,
  string? NextCursor,
  bool HasMore);

public sealed record SocialCommentedPostDto(
  string Id,
  string Platform,
  string AccountId,
  string? AccountUsername,
  string? Content,
  string? Picture,
  string? Permalink,
  DateTimeOffset? CreatedTime,
  int CommentCount,
  int LikeCount);

public sealed record SocialCommentListDto(
  IReadOnlyList<SocialCommentDto> Items,
  string? NextCursor,
  bool HasMore);

public sealed record SocialCommentDto(
  string Id,
  string? ParentId,
  SocialCommentAuthorDto? Author,
  string? Message,
  DateTimeOffset? CreatedTime,
  int LikeCount,
  int ReplyCount,
  string? Platform,
  string? Url,
  bool CanReply,
  bool CanDelete,
  bool CanHide,
  bool IsHidden,
  IReadOnlyList<SocialCommentDto> Replies);

public sealed record SocialCommentAuthorDto(
  string? Id,
  string? Name,
  string? Username,
  string? Picture,
  bool IsOwner);

public sealed record CreateSocialCommentReplyRequest(
  string AccountId,
  string Message,
  string? CommentId = null);

public sealed record ToggleSocialCommentHiddenRequest(bool IsHidden);

public sealed record MarkSocialConversationReadRequest(string AccountId);

public sealed record SocialActionResultDto(bool Success, string? Message, string? Id = null);

public sealed record SocialConversationListDto(
  IReadOnlyList<SocialConversationDto> Items,
  string? NextCursor,
  bool HasMore);

public sealed record SocialConversationDto(
  string Id,
  string Platform,
  string AccountId,
  string? AccountUsername,
  string? ParticipantId,
  string? ParticipantName,
  string? ParticipantPicture,
  string? LastMessage,
  DateTimeOffset? UpdatedTime,
  string? Status,
  int? UnreadCount,
  string? Url);

public sealed record SocialMessageListDto(
  IReadOnlyList<SocialMessageDto> Items,
  string? NextCursor,
  bool HasMore);

public sealed record SocialMessageDto(
  string Id,
  string ConversationId,
  string AccountId,
  string? Platform,
  string? Text,
  string? SenderId,
  string? SenderName,
  string Direction,
  DateTimeOffset? CreatedAt,
  IReadOnlyList<SocialMessageAttachmentDto> Attachments);

public sealed record SocialMessageAttachmentDto(
  string? Id,
  string? Type,
  string? Url,
  string? FileName,
  string? PreviewUrl);

public sealed record SendSocialMessageRequest(
  string AccountId,
  string? Message,
  string? AttachmentUrl,
  string? AttachmentType);

public sealed record SocialMediaUploadDto(
  string PublicUrl,
  string ObjectKey,
  string FileName,
  string ContentType,
  long FileSize);

public sealed record SocialMediaPresignRequest(
  string FileName,
  string ContentType,
  long? FileSize = null);

public sealed record SocialMediaPresignDto(
  string UploadUrl,
  string PublicUrl,
  DateTimeOffset? Expires);
