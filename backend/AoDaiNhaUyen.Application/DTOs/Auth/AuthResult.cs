namespace AoDaiNhaUyen.Application.DTOs.Auth;

public sealed class AuthResult<T>
{
  private AuthResult(bool succeeded, T? value, string? errorCode, string? errorMessage)
  {
    Succeeded = succeeded;
    Value = value;
    ErrorCode = errorCode;
    ErrorMessage = errorMessage;
  }

  public bool Succeeded { get; }
  public T? Value { get; }
  public string? ErrorCode { get; }
  public string? ErrorMessage { get; }

  public static AuthResult<T> Success(T value) => new(true, value, null, null);

  public static AuthResult<T> Failure(string errorCode, string errorMessage) =>
    new(false, default, errorCode, errorMessage);
}
