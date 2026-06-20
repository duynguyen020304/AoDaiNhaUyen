namespace AoDaiNhaUyen.Application.Exceptions;

public sealed class ZernioApiException : Exception
{
  public ZernioApiException(
    string message,
    string errorCode = "zernio_api_error",
    int? statusCode = null,
    TimeSpan? retryAfter = null,
    Exception? innerException = null) : base(message, innerException)
  {
    ErrorCode = errorCode;
    StatusCode = statusCode;
    RetryAfter = retryAfter;
  }

  public string ErrorCode { get; }
  public int? StatusCode { get; }
  public TimeSpan? RetryAfter { get; }
}
