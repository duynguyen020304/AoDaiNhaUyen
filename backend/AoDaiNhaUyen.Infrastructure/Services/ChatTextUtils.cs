using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AoDaiNhaUyen.Infrastructure.Services;

internal static partial class ChatTextUtils
{
  public static string Normalize(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return string.Empty;
    }

    var decomposed = value.Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(decomposed.Length);

    foreach (var character in decomposed)
    {
      if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
      {
        builder.Append(char.ToLowerInvariant(character));
      }
    }

    return builder
      .ToString()
      .Normalize(NormalizationForm.FormC)
      .Replace('đ', 'd');
  }

  public static decimal? TryExtractBudget(string message)
  {
    var normalized = Normalize(message);
    var match = BudgetRegex().Match(normalized);
    if (!match.Success)
    {
      return null;
    }

    if (!decimal.TryParse(match.Groups["amount"].Value.Replace(',', '.'), CultureInfo.InvariantCulture, out var amount))
    {
      return null;
    }

    var unit = match.Groups["unit"].Value;
    return unit switch
    {
      "tr" or "trieu" => amount * 1_000_000m,
      "k" or "nghin" => amount * 1_000m,
      _ => amount
    };
  }

  public static IReadOnlyList<long> ResolveOrdinalReferences(string message, IReadOnlyList<long> shortlist)
  {
    if (shortlist.Count == 0)
    {
      return [];
    }

    var normalized = Normalize(message);
    var results = new List<long>();

    if (normalized.Contains("dau tien") || normalized.Contains("thu nhat"))
    {
      results.Add(shortlist[0]);
    }

    if (shortlist.Count > 1 && normalized.Contains("thu hai"))
    {
      results.Add(shortlist[1]);
    }

    if (shortlist.Count > 2 && normalized.Contains("thu ba"))
    {
      results.Add(shortlist[2]);
    }

    if (results.Count > 0)
    {
      return results.Distinct().ToList();
    }

    if (normalized.Contains("cac mau nay") || normalized.Contains("mau tren") || normalized.Contains("cac san pham tren") || normalized.Contains("may mau tren") || normalized.Contains("3 ao dai nay") || normalized.Contains("3 mau nay"))
    {
      var requestedCount = normalized.Contains("3 ") ? Math.Min(3, shortlist.Count) : shortlist.Count;
      return shortlist.Take(requestedCount).ToList();
    }

    return [];
  }

  [GeneratedRegex(@"(?<amount>\d+(?:[\.,]\d+)?)\s*(?<unit>trieu|tr|nghin|k)?", RegexOptions.IgnoreCase)]
  private static partial Regex BudgetRegex();
}
