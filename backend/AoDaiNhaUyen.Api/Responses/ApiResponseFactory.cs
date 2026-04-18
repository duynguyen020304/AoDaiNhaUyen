namespace AoDaiNhaUyen.Api.Responses;

public static class ApiResponseFactory
{
  public static ApiResponse<T> Success<T>(T data, string message = "Lấy dữ liệu thành công")
  {
    return new ApiResponse<T>(true, message, data, null, DateTime.UtcNow);
  }

  public static PaginatedApiResponse<T> PaginatedSuccess<T>(
    T data,
    int page,
    int pageSize,
    int totalItem,
    string message = "Lấy dữ liệu thành công")
  {
    var totalPage = pageSize <= 0 ? 1 : (int)Math.Ceiling((double)totalItem / pageSize);
    totalPage = totalPage <= 0 ? 1 : totalPage;

    return new PaginatedApiResponse<T>(
      true,
      message,
      data,
      page < totalPage,
      page > 1,
      totalPage,
      totalItem,
      null,
      DateTime.UtcNow);
  }

  public static ApiResponse<object> Failure(
    string message,
    string code,
    string errorMessage)
  {
    return new ApiResponse<object>(
      false,
      message,
      null,
      [new ApiError(code, errorMessage)],
      DateTime.UtcNow);
  }
}
