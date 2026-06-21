using System.Data;
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
    var values = ToDictionary(payloadJson);
    if (!await HasTemplateConfigColumnsAsync(cancellationToken))
    {
      var legacyTemplate = await FindLegacyTemplateAsync(templateKey, locale, cancellationToken);
      return legacyTemplate is null ? BuiltInTemplate(templateKey, values) : RenderLegacyTemplate(legacyTemplate, values);
    }

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

    if (template is null)
    {
      return BuiltInTemplate(templateKey, values);
    }

    var subject = ReplaceTokens(template.Subject, values);
    var codeTemplateType = SafeEmailTemplateRenderer.ResolveType(template.Key, template.TemplateType);
    if (codeTemplateType is not null)
    {
      return new RenderedEmail(
        subject,
        SafeEmailTemplateRenderer.Render(codeTemplateType, subject, template.Preheader, template.ConfigJson, values),
        SafeEmailTemplateRenderer.RenderText(subject, template.ConfigJson, values));
    }

    return new RenderedEmail(
      subject,
      ReplaceTokens(template.HtmlBody, values),
      template.TextBody is null ? null : ReplaceTokens(template.TextBody, values));
  }

  private sealed record LegacyTemplateRow(string Key, string Subject, string? Preheader, string HtmlBody, string? TextBody);

  private async Task<bool> HasTemplateConfigColumnsAsync(CancellationToken cancellationToken)
  {
    if (!dbContext.Database.IsRelational()) return true;

    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    try
    {
      if (shouldClose) await connection.OpenAsync(cancellationToken);
      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'email_templates'
          AND column_name IN ('template_type', 'config_json', 'is_system')
        """;
      var result = await command.ExecuteScalarAsync(cancellationToken);
      return Convert.ToInt32(result) == 3;
    }
    catch (Exception) when (!cancellationToken.IsCancellationRequested)
    {
      return true;
    }
    finally
    {
      if (shouldClose && connection.State == ConnectionState.Open) await connection.CloseAsync();
    }
  }

  private async Task<LegacyTemplateRow?> FindLegacyTemplateAsync(string templateKey, string locale, CancellationToken cancellationToken)
  {
    return await dbContext.EmailTemplates.AsNoTracking()
      .Where(x => x.Key == templateKey && x.IsActive && x.Locale == locale)
      .OrderByDescending(x => x.Version)
      .Select(x => new LegacyTemplateRow(x.Key, x.Subject, x.Preheader, x.HtmlBody, x.TextBody))
      .FirstOrDefaultAsync(cancellationToken)
      ?? await dbContext.EmailTemplates.AsNoTracking()
        .Where(x => x.Key == templateKey && x.IsActive)
        .OrderByDescending(x => x.Version)
        .Select(x => new LegacyTemplateRow(x.Key, x.Subject, x.Preheader, x.HtmlBody, x.TextBody))
        .FirstOrDefaultAsync(cancellationToken);
  }

  private static RenderedEmail RenderLegacyTemplate(LegacyTemplateRow template, IReadOnlyDictionary<string, string> values)
  {
    var subject = ReplaceTokens(template.Subject, values);
    var codeTemplateType = SafeEmailTemplateRenderer.ResolveType(template.Key);
    if (codeTemplateType is not null)
    {
      return new RenderedEmail(
        subject,
        SafeEmailTemplateRenderer.Render(codeTemplateType, subject, template.Preheader, "{}", values),
        SafeEmailTemplateRenderer.RenderText(subject, "{}", values));
    }

    return new RenderedEmail(
      subject,
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

    var codeTemplateType = SafeEmailTemplateRenderer.ResolveType(templateKey);
    if (codeTemplateType is not null)
    {
      return new RenderedEmail(
        subject,
        SafeEmailTemplateRenderer.Render(codeTemplateType, subject, values.GetValueOrDefault("preheader", string.Empty), "{}", values),
        SafeEmailTemplateRenderer.RenderText(subject, "{}", values));
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
