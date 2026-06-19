namespace AoDaiNhaUyen.Application.Exceptions;

public sealed class FacebookApiException : Exception
{
  public FacebookApiException(
    string message,
    string errorCode = "facebook_api_error",
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
