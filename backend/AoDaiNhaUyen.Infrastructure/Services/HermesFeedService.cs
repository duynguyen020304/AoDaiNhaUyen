using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed partial class HermesFeedService(AppDbContext dbContext) : IHermesFeedService
{
  public async Task<HermesFeedSnapshotResponse> GetRecentFeedAsync(int maxItems, CancellationToken cancellationToken)
  {
    var take = Math.Clamp(maxItems, 1, 100);
    var events = await dbContext.HermesEventOutbox.AsNoTracking()
      .OrderByDescending(x => x.OccurredAt)
      .ThenByDescending(x => x.CreatedAt)
      .Take(take)
      .ToListAsync(cancellationToken);

    var heartbeat = await GetLatestHeartbeatAsync(cancellationToken);
    if (events.Count == 0)
      return new HermesFeedSnapshotResponse(Array.Empty<HermesFeedItemResponse>(), heartbeat, DateTimeOffset.UtcNow);

    var eventIds = events.Select(x => x.Id).ToArray();
    var conversationIds = events.Select(x => x.Id.ToString("N")).ToHashSet(StringComparer.Ordinal);
    var correlationIds = events.Select(x => x.CorrelationId).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);

    var runs = await dbContext.HermesRuns.AsNoTracking()
      .Where(x => x.ConversationId != null && (conversationIds.Contains(x.ConversationId) || correlationIds.Contains(x.ConversationId)))
      .OrderBy(x => x.StartedAt)
      .ToListAsync(cancellationToken);

    var runIds = runs.Select(x => x.Id).ToArray();
    var traces = await dbContext.HermesAgentTraceSteps.AsNoTracking()
      .Where(x => (x.EventOutboxId != null && eventIds.Contains(x.EventOutboxId.Value)) || (x.RunId != null && runIds.Contains(x.RunId.Value)))
      .OrderBy(x => x.StartedAt)
      .ToListAsync(cancellationToken);

    var reports = await dbContext.HermesReports.AsNoTracking()
      .Where(x => (x.RunId != null && runIds.Contains(x.RunId.Value)) || (x.CorrelationId != null && (correlationIds.Contains(x.CorrelationId) || conversationIds.Contains(x.CorrelationId))))
      .OrderBy(x => x.CreatedAt)
      .ToListAsync(cancellationToken);

    var items = events
      .OrderBy(x => x.OccurredAt)
      .Select(item =>
      {
        var itemConversationId = item.Id.ToString("N");
        var itemRuns = runs.Where(run => run.ConversationId == itemConversationId || (!string.IsNullOrWhiteSpace(item.CorrelationId) && run.ConversationId == item.CorrelationId)).ToArray();
        var itemRunIds = itemRuns.Select(x => x.Id).ToHashSet();
        return MapItem(
          item,
          itemRuns,
          traces.Where(step => step.EventOutboxId == item.Id || (step.RunId != null && itemRunIds.Contains(step.RunId.Value))).ToArray(),
          reports.Where(report => report.CorrelationId == itemConversationId || (!string.IsNullOrWhiteSpace(item.CorrelationId) && report.CorrelationId == item.CorrelationId) || (report.RunId != null && itemRunIds.Contains(report.RunId.Value))).ToArray());
      })
      .ToArray();

    return new HermesFeedSnapshotResponse(items, heartbeat, DateTimeOffset.UtcNow);
  }

  public async Task<HermesFeedHeartbeatResponse?> GetLatestHeartbeatAsync(CancellationToken cancellationToken)
  {
    return await dbContext.HermesHeartbeats.AsNoTracking()
      .OrderByDescending(x => x.RecordedAt)
      .Select(x => new HermesFeedHeartbeatResponse(x.RunnerName, x.Status, x.ActiveJobs, x.RecordedAt))
      .FirstOrDefaultAsync(cancellationToken);
  }

  private static HermesFeedItemResponse MapItem(HermesEventOutbox item, IReadOnlyCollection<HermesRun> runs, IReadOnlyCollection<HermesAgentTraceStep> traces, IReadOnlyCollection<HermesReport> reports)
  {
    var messages = new List<HermesFeedHermesMessageResponse>();

    if (item.Status == "pending")
      messages.Add(new HermesFeedHermesMessageResponse("thinking", "Đang chờ Hermes", "Event đã vào hàng đợi. Worker sẽ phân tích khi đến lượt.", item.OccurredAt, "pending", null));

    messages.AddRange(traces.Select(step => new HermesFeedHermesMessageResponse(
      MapTraceKind(step.Kind, step.Status), Redact(step.Title, 200), Redact(step.Error ?? step.Summary, 1200) ?? string.Empty, step.StartedAt, step.Status, null)));

    var reportRunIds = reports.Where(report => report.RunId.HasValue).Select(report => report.RunId!.Value).ToHashSet();
    var hasEventLevelReport = reports.Count > 0;

    messages.AddRange(runs.Where(run =>
      !string.IsNullOrWhiteSpace(run.Error) ||
      (!string.IsNullOrWhiteSpace(run.ResultPreview) && !reportRunIds.Contains(run.Id) && !hasEventLevelReport)
    ).Select(run => new HermesFeedHermesMessageResponse(
      string.IsNullOrWhiteSpace(run.Error) ? "thinking" : "error", run.Status == "completed" ? "Hermes đã phản hồi" : "Trạng thái Hermes", Redact(run.ResultPreview ?? run.Error, 1600) ?? string.Empty, run.CompletedAt ?? run.StartedAt, run.Status, null)));

    messages.AddRange(reports.Select(report => new HermesFeedHermesMessageResponse("report", Redact(report.Title, 200), Redact(report.Summary, 2000) ?? string.Empty, report.CreatedAt, report.Status, report.Severity)));

    if (item.Status is "failed" or "dead" && !string.IsNullOrWhiteSpace(item.LastError))
      messages.Add(new HermesFeedHermesMessageResponse("error", item.Status == "dead" ? "Hermes dừng retry" : "Hermes gặp lỗi", Redact(item.LastError, 1200) ?? string.Empty, item.ProcessedAt ?? item.ScheduledAt, item.Status, null));

    if (messages.Count == 0 && item.Status == "processing")
      messages.Add(new HermesFeedHermesMessageResponse("thinking", "Hermes đang xử lý", "Worker đã nhận event và đang tạo phân tích an toàn.", item.LockedAt ?? item.ScheduledAt, "processing", null));

    var runStatus = runs.OrderByDescending(x => x.StartedAt).FirstOrDefault()?.Status;
    return new HermesFeedItemResponse(item.Id, BuildStoreMessage(item), item.OccurredAt, item.EventType, item.Status, messages.OrderBy(x => x.Time).ToArray(), runStatus);
  }

  private static string MapTraceKind(string kind, string status)
  {
    if (status == "failed") return "error";
    return kind switch
    {
      "prompt_built" or "agent_request" or "agent_response" => "thinking",
      "report_created" => "report",
      "failed" => "error",
      _ => "step"
    };
  }

  private static string BuildStoreMessage(HermesEventOutbox item)
  {
    using var payload = TryParsePayload(item.PayloadJson);
    var root = payload?.RootElement;
    var id = ShortCode(item.AggregateId);
    var orderId = ReadString(root, "orderId", "order_id", "orderCode", "order_code") ?? id;
    var productName = ReadString(root, "productName", "product_name", "name", "title") ?? id;
    var promoCode = ReadString(root, "code", "promoCode", "promo_code") ?? id;
    var roleName = ReadString(root, "role", "roleName", "role_name") ?? id;
    var status = ReadString(root, "newStatus", "new_status", "status", "shipmentStatus", "shipment_status");
    var total = ReadDecimal(root, "total", "totalAmount", "total_amount", "grandTotal", "grand_total");
    var quantity = ReadInt(root, "itemCount", "item_count", "quantity", "qty", "stockQty", "stock_qty");
    var delta = ReadInt(root, "delta", "change", "stockDelta", "stock_delta");

    return item.EventType switch
    {
      "checkout_completed" => $"🛒 Khách vừa đặt đơn {ShortCode(orderId)}{FormatOrderSuffix(quantity, total)}",
      "order_status_changed" => $"📦 Đơn {ShortCode(orderId)} chuyển sang {Safe(status) ?? "trạng thái mới"}",
      "shipment_created" => $"🚚 Đơn {ShortCode(orderId)} vừa tạo vận đơn",
      "shipment_status_changed" => $"🚚 Vận đơn {ShortCode(orderId)} chuyển sang {Safe(status) ?? "trạng thái mới"}",
      "high_value_order_flagged" => $"⚠️ Đơn {ShortCode(orderId)} giá trị cao{FormatMoneySuffix(total)}",
      "cod_high_risk_flagged" => $"📞 Đơn COD {ShortCode(orderId)} cần xác nhận trước giao{FormatMoneySuffix(total)}",
      "vip_status_achieved" => $"💎 Khách vừa chạm mốc VIP từ đơn {ShortCode(orderId)}",
      "margin_negative_profit_warning" => $"💸 Đơn {ShortCode(orderId)} có nguy cơ âm lợi nhuận",
      "delivery_failed_alert" => $"🚨 Vận đơn {ShortCode(orderId)} giao thất bại cần chăm sóc ngay",
      "cod_rts_alert" => $"📞 Đơn COD {ShortCode(orderId)} có rủi ro hoàn/giao thất bại cần xử lý",
      "discount_threshold_exceeded" => $"💸 Đơn {ShortCode(orderId)} dùng giảm giá cao cần kiểm tra margin",
      "custom_tailoring_order_completed" => $"🧵 Đơn may đo {ShortCode(orderId)} vừa hoàn tất checkout",
      "product_created" => $"✨ Sản phẩm {Quote(productName)} vừa được tạo",
      "product_updated" => $"📝 Sản phẩm {Quote(productName)} đã được cập nhật",
      "product_deleted" => $"🗑️ Sản phẩm {Quote(productName)} đã bị xóa",
      "product_stock_changed" => $"📊 Tồn kho {Quote(productName)} thay đổi{FormatDeltaSuffix(delta)}",
      "stock_out_critical" => $"⛔ Sản phẩm {Quote(productName)} đã hết hàng",
      "low_stock" => $"⚠️ Sản phẩm {Quote(productName)} sắp hết{FormatQuantitySuffix(quantity)}",
      "stock_replenished" => $"✅ Sản phẩm {Quote(productName)} vừa được bổ sung tồn kho",
      "promo_created" => $"🏷️ Mã khuyến mãi {Quote(promoCode)} vừa được tạo",
      "promo_updated" => $"🏷️ Mã khuyến mãi {Quote(promoCode)} đã được cập nhật",
      "promo_disabled" => $"🏷️ Mã khuyến mãi {Quote(promoCode)} đã tắt",
      "admin_user_changed" => "👤 Tài khoản admin vừa thay đổi",
      "role_permissions_changed" => $"🔐 Quyền role {Quote(roleName)} vừa thay đổi",
      "media_uploaded" => "🖼️ Media mới được tải lên cửa hàng",
      "media_deleted" => "🗑️ Media đã bị xóa khỏi cửa hàng",
      "content_published" => $"📰 Nội dung {Quote(productName)} vừa xuất bản",
      "content_updated" => $"📰 Nội dung {Quote(productName)} đã được cập nhật",
      "blog_seo_opportunity" => $"🔎 Bài blog {Quote(productName)} cần tối ưu SEO",
      "pending_review_needed" => $"💬 Bình luận/đánh giá mới cho {Quote(productName)} cần Hermes xem xét",
      "negative_review_detected" => $"⚠️ Đánh giá tiêu cực về {Quote(productName)} cần xử lý",
      "review_recovery_initiated" => $"💝 Đã bắt đầu chăm sóc phục hồi trải nghiệm cho {Quote(productName)}",
      "bad_review_recovery_stats" => "💝 Thống kê chăm sóc đánh giá xấu vừa được cập nhật",
      "review_moderation_changed" => $"🛡️ Trạng thái đánh giá về {Quote(productName)} vừa thay đổi",
      "social_metrics_snapshot_created" => $"📈 Snapshot social {Quote(productName)} vừa được cập nhật",
      "social_engagement_milestone" => $"📈 Kênh social đạt mốc tương tác mới cho {Quote(productName)}",
      "social_engagement_anomaly" => $"⚠️ Social có biến động bất thường cho {Quote(productName)}",
      "social_campaign_performance_changed" => $"📣 Hiệu suất chiến dịch social {Quote(productName)} vừa thay đổi",
      "social_comment_received" => "💬 Có bình luận social mới cần kiểm tra",
      "social_message_received" => "💬 Có tin nhắn social mới cần chăm sóc",
      "critical_email_dead" => "📧 Email quan trọng đã hết lượt retry cần xử lý ngay",
      "email_template_created" => "📧 Template email mới được tạo",
      "email_template_updated" => "📧 Template email vừa được cập nhật",
      "email_campaign_created" => "📣 Campaign email mới được tạo",
      "email_campaign_scheduled" => "📣 Campaign email vừa được lên lịch",
      "email_campaign_changed" => "📧 Campaign email vừa thay đổi",
      "hermes_config_changed" => "⚙️ Cấu hình Hermes vừa thay đổi",
      _ => $"🏪 {item.EventType} cho {item.AggregateType}/{ShortCode(item.AggregateId)}"
    };
  }

  private static JsonDocument? TryParsePayload(string payloadJson)
  {
    try { return string.IsNullOrWhiteSpace(payloadJson) ? null : JsonDocument.Parse(payloadJson); }
    catch (JsonException) { return null; }
  }

  private static string? ReadString(JsonElement? root, params string[] names)
  {
    if (root is null || root.Value.ValueKind != JsonValueKind.Object) return null;
    foreach (var name in names)
      if (TryFind(root.Value, name, out var value))
        return value.ValueKind == JsonValueKind.String ? Safe(value.GetString()) : Safe(value.ToString());
    return null;
  }

  private static decimal? ReadDecimal(JsonElement? root, params string[] names)
  {
    if (root is null || root.Value.ValueKind != JsonValueKind.Object) return null;
    foreach (var name in names)
    {
      if (!TryFind(root.Value, name, out var value)) continue;
      if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
      if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number)) return number;
    }
    return null;
  }

  private static int? ReadInt(JsonElement? root, params string[] names)
  {
    if (root is null || root.Value.ValueKind != JsonValueKind.Object) return null;
    foreach (var name in names)
    {
      if (!TryFind(root.Value, name, out var value)) continue;
      if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
      if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) return number;
    }
    return null;
  }

  private static bool TryFind(JsonElement root, string name, out JsonElement value)
  {
    if (root.TryGetProperty(name, out value)) return true;
    foreach (var property in root.EnumerateObject())
      if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
      {
        value = property.Value;
        return true;
      }
    value = default;
    return false;
  }

  private static string ShortCode(string value)
  {
    var safe = Safe(value) ?? "event";
    if (Guid.TryParse(safe, out var id)) return $"#{id.ToString("N")[..8]}";
    return safe.Length <= 18 ? safe : $"{safe[..17]}…";
  }

  private static string Quote(string value) => $"'{ShortCode(value)}'";
  private static string? Safe(string? value) => string.IsNullOrWhiteSpace(value) ? null : Redact(value.Trim(), 120);

  private static string FormatOrderSuffix(int? quantity, decimal? total)
  {
    var parts = new List<string>();
    if (quantity is > 0) parts.Add($"{quantity} SP");
    if (total is > 0) parts.Add(FormatMoney(total.Value));
    return parts.Count == 0 ? string.Empty : $" - {string.Join(", ", parts)}";
  }

  private static string FormatMoneySuffix(decimal? total) => total is > 0 ? $": {FormatMoney(total.Value)}" : string.Empty;
  private static string FormatQuantitySuffix(int? quantity) => quantity is not null ? $" (còn {quantity})" : string.Empty;
  private static string FormatDeltaSuffix(int? delta) => delta is not null ? $": {(delta > 0 ? "+" : string.Empty)}{delta}" : string.Empty;

  private static string FormatMoney(decimal value)
  {
    if (value >= 1_000_000) return $"{Math.Round(value / 1_000_000M, 1):0.#}tr₫";
    if (value >= 1_000) return $"{Math.Round(value / 1_000M, 0):0}k₫";
    return $"{value:0}₫";
  }

  private static string? Redact(string? value, int max)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var redacted = SecretPattern().Replace(value, "$1[đã che]");
    redacted = EmailPattern().Replace(redacted, MaskEmail);
    redacted = PhonePattern().Replace(redacted, "[sđt đã che]");
    redacted = AddressPattern().Replace(redacted, "$1[địa chỉ đã che]");
    redacted = redacted.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
    return redacted.Length <= max ? redacted : redacted[..Math.Max(0, max - 1)] + "…";
  }

  private static string MaskEmail(Match match)
  {
    var value = match.Value;
    var at = value.IndexOf('@');
    return at <= 1 ? "[email đã che]" : value[..1] + "***" + value[at..];
  }

  [GeneratedRegex("(?i)(api[_-]?key|token|secret|password|authorization|bearer)\\s*[:=]\\s*([^\\s,;\"']+)")]
  private static partial Regex SecretPattern();

  [GeneratedRegex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
  private static partial Regex EmailPattern();

  [GeneratedRegex("(?i)((?:address|dia chi|địa chỉ)\\s*[:=]\\s*)([^,.;]{6,120})")]
  private static partial Regex AddressPattern();

  [GeneratedRegex("(?<!\\d)(?:\\+?84|0)(?:[\\s.-]?\\d){8,10}(?!\\d)")]
  private static partial Regex PhonePattern();
}
