namespace AoDaiNhaUyen.Application.DTOs.Facebook;

public sealed record CreateFacebookPostRequest(
  string Message,
  string? Link,
  DateTimeOffset? ScheduledPublishTime,
  bool Published = true,
  IReadOnlyList<string>? MediaUrls = null);

public sealed record UpdateFacebookPostRequest(string Message);

public sealed record FacebookPostDto(
  string Id,
  string? Message,
  DateTimeOffset? CreatedTime,
  DateTimeOffset? UpdatedTime,
  string? PermalinkUrl,
  string? FullPicture,
  bool? IsPublished,
  DateTimeOffset? ScheduledPublishTime,
  string? StatusType,
  string? Type);

public sealed record FacebookPostListDto(
  IReadOnlyList<FacebookPostDto> Items,
  string? BeforeCursor,
  string? AfterCursor,
  string? NextUrl);

public sealed record FacebookPublishResultDto(
  string Id,
  string? PostId,
  string? PermalinkUrl);

public sealed record FacebookDeleteResultDto(bool Success);
