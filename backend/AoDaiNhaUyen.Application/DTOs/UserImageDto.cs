namespace AoDaiNhaUyen.Application.DTOs;

/// <summary>
/// Ảnh do người dùng tạo.
/// </summary>
public sealed record UserImageDto(
  Guid Id,
  string ObjectKey,
  string Url,
  string Kind,
  string MimeType,
  string? OriginalFileName,
  long FileSizeBytes,
  string SourceType,
  DateTimeOffset CreatedAt);

/// <summary>
/// Phân trang ảnh người dùng.
/// </summary>
public sealed record UserImageListDto(
  IReadOnlyList<UserImageDto> Items,
  int Page,
  int PageSize,
  int TotalItems,
  int TotalPages);

/// <summary>
/// Thống kê ảnh cho admin.
/// </summary>
public sealed record MediaStatsDto(
  int TotalImages,
  long TotalSizeBytes,
  int ChatImages,
  int AiTryOnImages);
