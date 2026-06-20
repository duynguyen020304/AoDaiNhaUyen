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
  string? PlatformPostUrl);

public sealed record SocialPostDto(
  string Id,
  string? Content,
  string? Status,
  DateTimeOffset? ScheduledFor,
  DateTimeOffset? PublishedAt,
  string? PlatformPostUrl,
  IReadOnlyList<SocialPostPlatformDto> Platforms);

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
