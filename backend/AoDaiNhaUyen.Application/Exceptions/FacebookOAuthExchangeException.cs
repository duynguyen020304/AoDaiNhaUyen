namespace AoDaiNhaUyen.Application.Exceptions;

public sealed class FacebookOAuthExchangeException : Exception
{
  public FacebookOAuthExchangeException(string message)
    : base(message)
  {
  }
}
