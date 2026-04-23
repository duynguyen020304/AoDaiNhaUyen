namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IStylistFallbackTextService
{
  string Pick(string theme);
  string Pick(string theme, params (string Key, string Value)[] placeholders);
}
