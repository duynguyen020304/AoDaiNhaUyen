namespace AoDaiNhaUyen.Application.DTOs.Facebook;

public sealed record ConnectFacebookPageRequest(
  string PageId,
  string PageAccessToken,
  string? PageName);

public sealed record FacebookConnectionDto(
  string PageId,
  string? PageName,
  string TokenLast4,
  DateTimeOffset? ExpiresAt,
  DateTimeOffset? LastValidatedAt,
  bool IsActive);

public sealed record FacebookPageInfoDto(
  string PageId,
  string Name,
  string? Category,
  string? Link);
