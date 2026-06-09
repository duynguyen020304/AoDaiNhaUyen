using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.Interfaces.Services;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed partial class PromptRedactionService : IPromptRedactionService
{
  public string Redact(string? value, int maxLength = 2000)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;

    var redacted = value;
    redacted = EmailRegex().Replace(redacted, "[EMAIL_REDACTED]");
    redacted = PhoneRegex().Replace(redacted, "[PHONE_REDACTED]");
    redacted = JwtRegex().Replace(redacted, "[TOKEN_REDACTED]");
    redacted = ApiKeyRegex().Replace(redacted, "[SECRET_REDACTED]");
    redacted = SignedUrlRegex().Replace(redacted, "$1[QUERY_REDACTED]");
    redacted = Base64Regex().Replace(redacted, "[BASE64_REDACTED]");

    return redacted.Length <= maxLength ? redacted : redacted[..maxLength] + "…";
  }

  [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex EmailRegex();

  [GeneratedRegex(@"(?<!\d)(?:\+?84|0)(?:\s|\.|-)?(?:\d(?:\s|\.|-)?){8,10}(?!\d)", RegexOptions.CultureInvariant)]
  private static partial Regex PhoneRegex();

  [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.CultureInvariant)]
  private static partial Regex JwtRegex();

  [GeneratedRegex("(?i)(api[_-]?key|secret|token|password|authorization)\\s*[:=]\\s*[^\\s,;\\]\\}\\\"']+")]
  private static partial Regex ApiKeyRegex();

  [GeneratedRegex(@"(https?://[^\s?]+)\?[^\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex SignedUrlRegex();

  [GeneratedRegex(@"[A-Za-z0-9+/]{160,}={0,2}", RegexOptions.CultureInvariant)]
  private static partial Regex Base64Regex();
}
