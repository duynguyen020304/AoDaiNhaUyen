namespace AoDaiNhaUyen.Api.Responses;

public sealed record ApiResponse<T>(
  bool Success,
  string Message,
  T? Data,
  IReadOnlyList<ApiError>? Errors,
  DateTime Timestamp);

public sealed record PaginatedApiResponse<T>(
  bool Success,
  string Message,
  T? Data,
  bool HasNextPage,
  bool HasPreviousPage,
  int TotalPage,
  int TotalItem,
  IReadOnlyList<ApiError>? Errors,
  DateTime Timestamp);
