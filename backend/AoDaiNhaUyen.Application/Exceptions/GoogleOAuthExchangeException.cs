namespace AoDaiNhaUyen.Application.Exceptions;

public sealed class GoogleOAuthExchangeException : Exception
{
  public GoogleOAuthExchangeException(string message)
    : base(message)
  {
  }
}
