using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class HermesEventProcessor(
  IHttpClientFactory httpClientFactory,
  IOptions<HermesAgentOptions> agentOptions,
  IOptions<HermesOutboxOptions> outboxOptions,
  AppDbContext dbContext,
  ILogger<HermesEventProcessor> logger) : IHermesEventProcessor
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private readonly HermesAgentOptions _agentOptions = agentOptions.Value;
  private readonly HermesOutboxOptions _outboxOptions = outboxOptions.Value;

  public async Task ProcessAsync(HermesEventOutbox item, CancellationToken cancellationToken)
  {
    ValidatePayload(item.PayloadJson);

    var run = CreateRun(item);
    dbContext.HermesRuns.Add(run);
    await dbContext.SaveChangesAsync(cancellationToken);

    await AddTraceAsync(item.Id, run.Id, "prompt_built", "Chuẩn bị phân tích", "Hermes đang đọc sự kiện.", "success", null, cancellationToken);

    if (_outboxOptions.DryRun)
    {
      await CompleteRunAsync(run, "completed", "Hermes outbox dry-run: event accepted but not sent.", null, cancellationToken);
      logger.LogInformation("Hermes outbox dry-run processed event {EventId} ({EventType})", item.Id, item.EventType);
      return;
    }

    if (!IsApiConfigured())
    {
      const string message = "Hermes API server chưa cấu hình.";
      await CompleteRunAsync(run, "failed", null, message, cancellationToken);
      throw new InvalidOperationException(message);
    }

    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromMinutes(6);
    client.BaseAddress = new Uri(_agentOptions.ApiServerUrl!, UriKind.Absolute);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _agentOptions.ApiServerKey);

    var payload = new
    {
      model = "hermes-agent",
      input = BuildInput(item),
      store = true,
      conversation = $"aodai-admin-event-{item.Id:N}",
      metadata = new
      {
        item.Id,
        item.EventType,
        item.AggregateType,
        item.AggregateId,
        item.CorrelationId
      }
    };

    await AddTraceAsync(item.Id, run.Id, "agent_request", "Đang phân tích", "Hermes đang đánh giá sự kiện.", "running", null, cancellationToken);

    using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
    using var response = await client.PostAsync(_outboxOptions.EventPath, content, cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      await AddTraceAsync(item.Id, run.Id, "failed", "Hermes trả lỗi", $"Hermes API returned {(int)response.StatusCode}.", "failed", body, cancellationToken);
      await CompleteRunAsync(run, "failed", null, $"Hermes API returned {(int)response.StatusCode}: {body}", cancellationToken);
      throw new InvalidOperationException($"Hermes API returned {(int)response.StatusCode}: {body}");
    }

    var result = ExtractAssistantText(body);
    await AddTraceAsync(item.Id, run.Id, "agent_response", "Phân tích xong", "Hermes đã hoàn thành đánh giá.", "success", null, cancellationToken);
    await RecordAgentReportAsync(item, run.Id, result, cancellationToken);
    await CompleteRunAsync(run, "completed", result, null, cancellationToken);
  }

  private async Task AddTraceAsync(Guid eventId, Guid runId, string kind, string title, string summary, string status, string? error, CancellationToken cancellationToken)
  {
    var now = DateTimeOffset.UtcNow;
    dbContext.HermesAgentTraceSteps.Add(new HermesAgentTraceStep
    {
      Id = Guid.NewGuid(),
      EventOutboxId = eventId,
      RunId = runId,
      Kind = kind,
      Title = title,
      Summary = summary,
      Status = status,
      StartedAt = now,
      CompletedAt = status == "running" ? null : now,
      Error = NormalizeOptionalText(error),
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task RecordAgentReportAsync(HermesEventOutbox item, Guid runId, string result, CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    var agentSummary = NormalizeAgentReportText(result);
    if (string.IsNullOrWhiteSpace(agentSummary))
    {
      logger.LogWarning("Hermes event {EventId} completed without agent report text; skipping saved report.", item.Id);
      return;
    }

    var profile = BuildReportProfile(item);
    var title = $"{profile.TitlePrefix}: {ShortCode(item.AggregateId)}";

    dbContext.HermesReports.Add(new HermesReport
    {
      Id = Guid.NewGuid(),
      ReportType = profile.ReportType,
      Severity = profile.Severity,
      Title = Limit(title, 200),
      Summary = Limit(agentSummary, 4000),
      PayloadJson = BuildReportPayload(item, profile, result),
      Source = "hermes_agent",
      CorrelationId = item.Id.ToString("N"),
      RunId = runId,
      Status = "open",
      CreatedAt = now,
      UpdatedAt = now
    });

    dbContext.HermesAgentTraceSteps.Add(new HermesAgentTraceStep
    {
      Id = Guid.NewGuid(),
      EventOutboxId = item.Id,
      RunId = runId,
      Kind = "report_created",
      Title = "Đã tạo báo cáo",
      Summary = "Hermes đã lưu báo cáo chủ động cho admin.",
      Status = "success",
      StartedAt = DateTimeOffset.UtcNow,
      CompletedAt = DateTimeOffset.UtcNow,
      CreatedAt = now,
      UpdatedAt = now
    });

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static ReportProfile BuildReportProfile(HermesEventOutbox item)
  {
    var eventType = item.EventType.ToLowerInvariant();
    if (eventType.Contains("negative", StringComparison.Ordinal) || eventType.Contains("low_stock", StringComparison.Ordinal) || eventType.Contains("disabled", StringComparison.Ordinal))
      return new ReportProfile("risk", "warning", "Cần xử lý rủi ro", "rủi ro cần xử lý", "Giảm thất thoát hoặc trải nghiệm xấu");

    if (eventType.Contains("high_value", StringComparison.Ordinal) || eventType.Contains("checkout", StringComparison.Ordinal) || eventType.Contains("promo", StringComparison.Ordinal))
      return new ReportProfile("revenue", "info", "Cơ hội doanh thu", "cơ hội tăng doanh thu", "Tăng AOV, upsell hoặc giữ chân khách");

    if (eventType.Contains("blog", StringComparison.Ordinal) || eventType.Contains("content", StringComparison.Ordinal))
      return new ReportProfile("seo", "info", "Cơ hội SEO", "cơ hội SEO", "Tăng organic traffic và internal link");

    if (eventType.Contains("email", StringComparison.Ordinal) || eventType.Contains("campaign", StringComparison.Ordinal))
      return new ReportProfile("crm", "info", "Cơ hội CRM", "cơ hội CRM", "Tăng repeat purchase và phân khúc khách");

    if (eventType.Contains("role", StringComparison.Ordinal) || eventType.Contains("admin", StringComparison.Ordinal) || eventType.Contains("config", StringComparison.Ordinal))
      return new ReportProfile("operations", "warning", "Rủi ro vận hành", "rủi ro vận hành", "Bảo vệ quyền admin và cấu hình kinh doanh");

    return new ReportProfile("growth", "info", "Gợi ý tăng trưởng", "tín hiệu tăng trưởng", "Biến tín hiệu cửa hàng thành hành động cụ thể");
  }

  private static string BuildReportPayload(HermesEventOutbox item, ReportProfile profile, string result)
  {
    var payload = new
    {
      agentGenerated = true,
      profile.ReportType,
      profile.Severity,
      item.EventType,
      item.AggregateType,
      item.AggregateId,
      item.CorrelationId,
      resultPreview = Limit(result, 1200)
    };
    return JsonSerializer.Serialize(payload, JsonOptions);
  }

  private HermesRun CreateRun(HermesEventOutbox item)
  {
    var now = DateTimeOffset.UtcNow;
    return new HermesRun
    {
      Id = Guid.NewGuid(),
      Status = "running",
      Trigger = "admin_event",
      ConversationId = item.Id.ToString("N"),
      PromptPreview = $"{item.EventType}:{item.AggregateType}:{item.AggregateId}",
      StartedAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    };
  }

  private async Task CompleteRunAsync(HermesRun run, string status, string? result, string? error, CancellationToken cancellationToken)
  {
    run.Status = status;
    run.ResultPreview = NormalizeOptionalText(result);
    run.Error = NormalizeOptionalText(error);
    run.CompletedAt = DateTimeOffset.UtcNow;
    run.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private static string BuildInput(HermesEventOutbox item) =>
    $"""
    ĐÂY LÀ SỰ KIỆN MỚI từ cửa hàng áo dài Nhã Uyên. Hãy PHÂN TÍCH CHỦ ĐỘNG.

    BẮT BUỘC: Trước khi kết luận, hãy dùng công cụ (read_file, execute_code, web_search)
    để KIỂM TRA dữ liệu thực tế của cửa hàng — không chỉ dựa vào payload sự kiện.
    Ví dụ: kiểm tra doanh thu gần đây, tồn kho, lượt xem blog, tỷ lệ mở email, v.v.

    <store_context>
    store: Áo Dài Nhã Uyên
    website: https://aodainhauyen.com
    market: Premium Vietnamese áo dài e-commerce
    target_audience: Women 25-45, Vietnam + overseas Vietnamese
    revenue_model: Direct e-commerce sales + custom tailoring
    key_products: Áo dài cách tân, áo dài cưới, áo dài truyền thống
    competition: Local tailors, online áo dài brands, fashion boutiques
    business_goal: Tăng doanh thu, tăng AOV, tăng repeat purchase, tăng SEO traffic, giảm thất thoát vận hành
    </store_context>

    <event_metadata>
    eventId: {item.Id}
    eventType: {item.EventType}
    aggregateType: {item.AggregateType}
    aggregateId: {item.AggregateId}
    correlationId: {item.CorrelationId}
    occurredAt: {item.OccurredAt:O}
    </event_metadata>

    <event_payload>
    {item.PayloadJson}
    </event_payload>
    """;

  private bool IsApiConfigured() =>
    Uri.TryCreate(_agentOptions.ApiServerUrl, UriKind.Absolute, out _) &&
    !string.IsNullOrWhiteSpace(_agentOptions.ApiServerKey);

  private static string ExtractAssistantText(string body)
  {
    if (string.IsNullOrWhiteSpace(body)) return "Hermes đã phân tích xong.";
    try
    {
      using var doc = JsonDocument.Parse(body);
      if (!doc.RootElement.TryGetProperty("output", out var output)) return "Hermes đã phân tích xong.";
      var builder = new StringBuilder();
      foreach (var item in output.EnumerateArray())
      {
        if (!item.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "message") continue;
        if (!item.TryGetProperty("content", out var content)) continue;
        foreach (var part in content.EnumerateArray())
        {
          if (!part.TryGetProperty("text", out var textProp)) continue;
          var text = textProp.GetString();
          if (!string.IsNullOrWhiteSpace(text))
          {
            if (builder.Length > 0) builder.AppendLine();
            builder.Append(text);
          }
        }
      }
      var result = builder.ToString().Trim();
      return string.IsNullOrEmpty(result) ? "Hermes đã phân tích xong." : result;
    }
    catch (JsonException)
    {
      return "Hermes đã phân tích xong.";
    }
  }

  private static void ValidatePayload(string payloadJson)
  {
    using var _ = JsonDocument.Parse(payloadJson);
  }

  private static string NormalizeAgentReportText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    return value.Trim();
  }

  private static string? NormalizeOptionalText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return value.Trim();
  }

  private static string Limit(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..Math.Max(0, maxLength - 1)] + "…";
  }

  private static string ShortCode(string value)
  {
    if (string.IsNullOrWhiteSpace(value)) return "event";
    var trimmed = value.Trim();
    if (Guid.TryParse(trimmed, out var id)) return $"#{id.ToString("N")[..8]}";
    return trimmed.Length <= 18 ? trimmed : $"{trimmed[..17]}…";
  }

  private sealed record ReportProfile(string ReportType, string Severity, string TitlePrefix, string Impact, string PriorityReason);
}
