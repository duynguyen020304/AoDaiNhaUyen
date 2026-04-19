namespace AoDaiNhaUyen.Application.Exceptions;

public sealed class ZaloOAuthExchangeException : Exception
{
  public ZaloOAuthExchangeException(string message)
    : base(message)
  {
  }
}
