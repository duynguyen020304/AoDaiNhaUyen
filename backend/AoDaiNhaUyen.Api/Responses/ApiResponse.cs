namespace AoDaiNhaUyen.Api.Responses;

public sealed record ApiResponse<T>(
  bool Success,
  string Message,
  T? Data,
  IReadOnlyList<ApiError>? Errors,
  DateTime Timestamp);
