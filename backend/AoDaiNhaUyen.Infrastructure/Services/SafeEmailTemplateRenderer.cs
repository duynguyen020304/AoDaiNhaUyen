using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AoDaiNhaUyen.Infrastructure.Services;

internal static class SafeEmailTemplateRenderer
{
  public static string? ResolveType(string key, string? templateType = null)
  {
    if (templateType is "marketing.promo" or "marketing.newsletter" or "subscriber.welcome" or "order.confirmation") return templateType;
    return key switch
    {
      "marketing.promo" => "marketing.promo",
      "marketing.newsletter" => "marketing.newsletter",
      "marketing.welcome" => "subscriber.welcome",
      "subscriber.welcome" => "subscriber.welcome",
      "order.confirmation" => "order.confirmation",
      _ => null
    };
  }

  public static bool IsCodeManagedKey(string key) => ResolveType(key) is not null;

  public static string Render(string type, string subject, string? preheader, string? configJson, IReadOnlyDictionary<string, string> values)
  {
    var data = Merge(configJson, values);
    var body = type switch
    {
      "marketing.newsletter" => Newsletter(data),
      "subscriber.welcome" => Welcome(data),
      "order.confirmation" => Order(data),
      _ => Promo(data)
    };
    return Layout(subject, preheader, body, data);
  }

  public static string RenderText(string subject, string? configJson, IReadOnlyDictionary<string, string> values)
  {
    var data = Merge(configJson, values);
    return string.Join("\n\n", new[] { subject, V(data, "heading"), V(data, "intro"), V(data, "body"), V(data, "footerNote"), V(data, "ctaText"), V(data, "ctaUrl") }.Where(x => !string.IsNullOrWhiteSpace(x)));
  }

  private static string Promo(IReadOnlyDictionary<string, string> d) =>
    $"<h1 style=\"margin:0 0 16px;color:#7f1d1d;font-size:28px;line-height:1.25\">{H(V(d, "heading", "Ưu đãi dành riêng cho bạn"))}</h1>{P(V(d, "intro", "Khám phá ưu đãi mới nhất từ Áo Dài Nhã Uyên."))}{PM(V(d, "body", Strip(V(d, "bodyHtml"))))}{V(d, "attachmentsHtml")}{Btn(V(d, "ctaText", V(d, "ctaLabel", "Xem ngay")), V(d, "ctaUrl", "https://aodainhauyen.io.vn/products"))}{P(V(d, "footerNote", V(d, "expiryDate")), "#8a6f58", 13)}";

  private static string Newsletter(IReadOnlyDictionary<string, string> d) =>
    $"<h1 style=\"margin:0 0 16px;color:#7f1d1d;font-size:26px;line-height:1.25\">{H(V(d, "heading", "Bản tin Áo Dài Nhã Uyên"))}</h1>{P(V(d, "intro", "Những câu chuyện, bộ sưu tập và ưu đãi được chọn lọc cho bạn."))}{PM(V(d, "body", Strip(V(d, "bodyHtml"))))}{V(d, "attachmentsHtml")}{Btn(V(d, "ctaText", "Đọc thêm"), V(d, "ctaUrl", "https://aodainhauyen.com/blog"))}";

  private static string Welcome(IReadOnlyDictionary<string, string> d) =>
    $"<h1 style=\"margin:0 0 16px;color:#7f1d1d;font-size:28px;line-height:1.25\">{H(V(d, "heading", $"Chào mừng {V(d, "name", "bạn")} đến với Áo Dài Nhã Uyên"))}</h1>{P(V(d, "intro", "Cảm ơn bạn đã đăng ký nhận tin. Từ hôm nay, bạn sẽ nhận bộ sưu tập và ưu đãi mới nhất."))}{Btn(V(d, "ctaText", "Khám phá bộ sưu tập"), V(d, "ctaUrl", V(d, "shopUrl", "https://aodainhauyen.io.vn/products")))}";

  private static string Order(IReadOnlyDictionary<string, string> d) =>
    $"<h1 style=\"margin:0 0 16px;color:#7f1d1d;font-size:28px;line-height:1.25\">{H(V(d, "heading", "Xác nhận đơn hàng"))}</h1><p style=\"margin:0 0 16px;font-size:16px;color:#3f2a1f\"><strong>{H(V(d, "orderCode", "Đơn hàng của bạn"))}</strong></p>{PM(V(d, "body", "Chúng tôi đã nhận được đơn hàng và sẽ liên hệ khi đơn được xử lý."))}{Btn(V(d, "ctaText", "Xem đơn hàng"), V(d, "ctaUrl", "https://aodainhauyen.com/account/orders"))}";

  private static string Layout(string subject, string? preheader, string body, IReadOnlyDictionary<string, string> d)
  {
    var logoUrl = V(d, "logoUrl");
    var logo = string.IsNullOrWhiteSpace(logoUrl) ? "<div style=\"font-weight:700;color:#7f1d1d;font-size:18px\">Ao Dai Nha Uyen</div>" : $"<img src=\"{A(logoUrl)}\" alt=\"Ao Dai Nha Uyen\" width=\"160\" style=\"display:block;max-width:160px\" />";
    var unsubscribeUrl = V(d, "unsubscribeUrl");
    var unsubscribe = string.IsNullOrWhiteSpace(unsubscribeUrl) ? string.Empty : $"<p style=\"margin:12px 0 0;font-size:12px;color:#9ca3af\"><a href=\"{A(unsubscribeUrl)}\" style=\"color:#9ca3af\">Hủy đăng ký</a></p>";
    var b = new StringBuilder();
    b.Append("<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>").Append(H(subject)).Append("</title></head>")
      .Append("<body style=\"margin:0;background:#f6f0ec;font-family:Arial,Helvetica,sans-serif;color:#2f1f1a\"><div style=\"display:none;max-height:0;overflow:hidden;color:transparent\">").Append(H(preheader ?? string.Empty)).Append("</div>")
      .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#f6f0ec\"><tr><td align=\"center\" style=\"padding:32px 16px\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:640px;background:#fff;border-radius:22px;overflow:hidden;border:1px solid #ead7d7\">")
      .Append("<tr><td style=\"padding:28px 32px 18px;background:#fffaf7\">").Append(logo).Append("</td></tr><tr><td style=\"padding:8px 32px 32px\">").Append(body).Append("</td></tr>")
      .Append("<tr><td style=\"padding:22px 32px;background:#2f1f1a;color:#f8eee8;font-size:12px;line-height:1.6\">© Ao Dai Nha Uyen. Email được gửi theo đăng ký/đơn hàng của bạn.").Append(unsubscribe).Append("</td></tr></table></td></tr></table></body></html>");
    return b.ToString();
  }

  private static Dictionary<string, string> Merge(string? json, IReadOnlyDictionary<string, string> values)
  {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!string.IsNullOrWhiteSpace(json))
    {
      using var doc = JsonDocument.Parse(json);
      if (doc.RootElement.ValueKind == JsonValueKind.Object)
        foreach (var p in doc.RootElement.EnumerateObject()) result[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() ?? string.Empty : p.Value.ToString();
    }
    foreach (var item in values) result[item.Key] = item.Value;
    return result;
  }

  private static string Btn(string label, string url) => string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(url) ? string.Empty : $"<p style=\"margin:28px 0 0\"><a href=\"{A(url)}\" style=\"display:inline-block;background:#7f1d1d;color:#fff;padding:13px 22px;border-radius:999px;text-decoration:none;font-weight:700\">{H(label)}</a></p>";
  private static string P(string value, string color = "#4b342a", int size = 16) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"<p style=\"margin:0 0 16px;color:{color};font-size:{size}px;line-height:1.7\">{H(value)}</p>";
  private static string PM(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"<p style=\"margin:0 0 16px;color:#4b342a;font-size:16px;line-height:1.7\">{H(value).Replace("\n", "<br>", StringComparison.Ordinal)}</p>";
  private static string V(IReadOnlyDictionary<string, string> d, string key, string fallback = "") => d.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
  private static string Strip(string value) => Regex.Replace(value, "<.*?>", string.Empty).Trim();
  private static string H(string value) => HtmlEncoder.Default.Encode(value);
  private static string A(string value) => HtmlEncoder.Default.Encode(value);
}
