using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AoDaiNhaUyen.Mcp.Tools;

internal static partial class ToolValidation
{
  public static int Page(int page) => Math.Max(1, page);

  public static int PageSize(int pageSize) => Math.Clamp(pageSize, 1, 100);

  public static int Limit(int limit) => Math.Clamp(limit, 1, 100);

  public static int PeriodDays(int periodDays) => Math.Clamp(periodDays, 1, 365);

  public static int RevenuePeriod(int period) => period is 7 or 30 or 90 ? period : 7;

  public static string? Search(string? value) => TrimMax(value, 200);

  public static string? Description(string? value) => TrimMax(value, 2000);

  public static bool TryRequiredName(string? value, out string name, out string? error)
  {
    name = (value ?? string.Empty).Trim();
    if (name.Length == 0)
    {
      error = "Tên không được để trống.";
      return false;
    }

    if (name.Length > 200)
    {
      error = "Tên không được vượt quá 200 ký tự.";
      return false;
    }

    error = null;
    return true;
  }

  public static bool IsProductType(string value) =>
    IsOneOf(value, "ao_dai", "phu_kien");

  public static bool IsProductStatus(string value) =>
    IsOneOf(value, "draft", "active", "inactive");

  public static bool IsActiveStatus(string value) =>
    IsOneOf(value, "active", "inactive");

  public static bool IsUserRole(string value) =>
    IsOneOf(value, "admin", "customer");

  public static string Slugify(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return "untitled";

    var normalized = text.Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);
    foreach (var c in normalized)
    {
      if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        builder.Append(c);
    }

    var slug = InvalidSlugCharsRegex().Replace(builder.ToString(), "");
    slug = WhitespaceRegex().Replace(slug, "-");
    slug = RepeatedDashRegex().Replace(slug, "-");
    slug = slug.Trim('-').ToLowerInvariant();
    return slug.Length > 200 ? slug[..200] : slug.Length > 0 ? slug : "untitled";
  }

  private static string? TrimMax(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }

  private static bool IsOneOf(string value, params string[] allowed) =>
    allowed.Any(candidate => candidate.Equals(value, StringComparison.OrdinalIgnoreCase));

  [GeneratedRegex(@"[^a-z0-9\s-]", RegexOptions.IgnoreCase)]
  private static partial Regex InvalidSlugCharsRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespaceRegex();

  [GeneratedRegex("-+")]
  private static partial Regex RepeatedDashRegex();
}
