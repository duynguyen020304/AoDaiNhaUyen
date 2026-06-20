using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed partial class EmailTemplateService(AppDbContext dbContext) : IEmailTemplateService
{
  public async Task<RenderedEmail> RenderAsync(
    string templateKey,
    string payloadJson,
    string locale = "vi-VN",
    CancellationToken cancellationToken = default)
  {
    var template = await dbContext.EmailTemplates
      .AsNoTracking()
      .Where(x => x.Key == templateKey && x.IsActive && x.Locale == locale)
      .OrderByDescending(x => x.Version)
      .FirstOrDefaultAsync(cancellationToken)
      ?? await dbContext.EmailTemplates
        .AsNoTracking()
        .Where(x => x.Key == templateKey && x.IsActive)
        .OrderByDescending(x => x.Version)
        .FirstOrDefaultAsync(cancellationToken);

    var values = ToDictionary(payloadJson);
    if (template is null)
    {
      return BuiltInTemplate(templateKey, values);
    }

    return new RenderedEmail(
      ReplaceTokens(template.Subject, values),
      ReplaceTokens(template.HtmlBody, values),
      template.TextBody is null ? null : ReplaceTokens(template.TextBody, values));
  }

  private static Dictionary<string, string> ToDictionary(string payloadJson)
  {
    using var document = JsonDocument.Parse(payloadJson);
    return document.RootElement.EnumerateObject()
      .ToDictionary(x => x.Name, x => x.Value.ValueKind == JsonValueKind.String ? x.Value.GetString() ?? string.Empty : x.Value.ToString());
  }

  private static RenderedEmail BuiltInTemplate(string templateKey, IReadOnlyDictionary<string, string> values)
  {
    var subject = values.GetValueOrDefault("subject", templateKey);
    var trustedHtml = values.GetValueOrDefault("trustedHtmlBody", string.Empty);
    if (AllowsTrustedHtmlBody(templateKey) && !string.IsNullOrWhiteSpace(trustedHtml))
    {
      return new RenderedEmail(subject, trustedHtml, null);
    }

    return templateKey switch
    {
      "marketing.confirm_subscription" => new RenderedEmail(
        "Xác nhận nhận tin từ Ao Dai Nha Uyen",
        $"<p>Chào bạn,</p><p>Vui lòng xác nhận đăng ký nhận tin:</p><p><a href=\"{values.GetValueOrDefault("confirmUrl", "#")}\">Xác nhận đăng ký</a></p>",
        null),
      "hermes.single_email" => new RenderedEmail(
        subject,
        BuildSingleEmailHtml(values),
        values.GetValueOrDefault("body", values.GetValueOrDefault("intro", subject))),
      _ => new RenderedEmail(subject, $"<p>{subject}</p>", null)
    };
  }

  private static string BuildSingleEmailHtml(IReadOnlyDictionary<string, string> values)
  {
    var intro = HtmlEncoder.Default.Encode(values.GetValueOrDefault("intro", string.Empty));
    var body = HtmlEncoder.Default.Encode(values.GetValueOrDefault("body", string.Empty)).Replace("\n", "<br>", StringComparison.Ordinal);
    var ctaLabel = HtmlEncoder.Default.Encode(values.GetValueOrDefault("ctaLabel", string.Empty));
    var ctaUrl = HtmlEncoder.Default.Encode(values.GetValueOrDefault("ctaUrl", string.Empty));
    var builder = new System.Text.StringBuilder();
    if (!string.IsNullOrWhiteSpace(intro)) builder.Append("<p>").Append(intro).Append("</p>");
    if (!string.IsNullOrWhiteSpace(body)) builder.Append("<p>").Append(body).Append("</p>");
    if (!string.IsNullOrWhiteSpace(ctaLabel) && !string.IsNullOrWhiteSpace(ctaUrl))
      builder.Append("<p style=\"margin:24px 0\"><a href=\"").Append(ctaUrl).Append("\" style=\"display:inline-block;background:#7f1d1d;color:#fff;padding:12px 18px;border-radius:999px;text-decoration:none;font-weight:700\">").Append(ctaLabel).Append("</a></p>");
    return builder.Length == 0 ? $"<p>{HtmlEncoder.Default.Encode(values.GetValueOrDefault("subject", "Thông báo từ Áo Dài Nhã Uyên"))}</p>" : builder.ToString();
  }

  private static bool AllowsTrustedHtmlBody(string templateKey)
  {
    return templateKey is "auth.verify_email" or "auth.reset_password" or "order.invoice";
  }

  private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> values)
  {
    return TokenRegex().Replace(template, match =>
    {
      var key = match.Groups[1].Value;
      if (!values.TryGetValue(key, out var value))
      {
        return string.Empty;
      }

      return key.EndsWith("Html", StringComparison.OrdinalIgnoreCase) || key.EndsWith("HtmlBody", StringComparison.OrdinalIgnoreCase)
        ? value
        : HtmlEncoder.Default.Encode(value);
    });
  }

  [GeneratedRegex(@"{{\s*([a-zA-Z0-9_]+)\s*}}")]
  private static partial Regex TokenRegex();
}
