using System.Text;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class HermesReportCompressorService(
  IAdminLlmProvider llm,
  IOptions<HermesOutboxOptions> options,
  ILogger<HermesReportCompressorService> logger) : IHermesReportCompressorService
{
  private readonly HermesOutboxOptions _options = options.Value;

  private static readonly object CompressionSchema = new
  {
    type = "object",
    properties = new Dictionary<string, object?>
    {
      ["title"] = new { type = "string" },
      ["summary"] = new { type = "string" },
      ["severity"] = new { type = "string" },
      ["reportType"] = new { type = "string" },
      ["keyFindings"] = new { type = "array", items = new { type = "string" } },
      ["recommendedActions"] = new { type = "array", items = new { type = "string" } },
      ["markdown"] = new { type = "string" }
    },
    required = new[] { "title", "summary", "severity", "reportType", "keyFindings", "recommendedActions", "markdown" }
  };

  public async Task<HermesCompressedReportResult> CompressAsync(
    IReadOnlyList<HermesPartialReportInput> partialReports,
    CancellationToken cancellationToken)
  {
    if (partialReports.Count == 0)
    {
      return new HermesCompressedReportResult(
        "Báo cáo Hermes",
        "Hermes không tạo được báo cáo thành phần.",
        "warning",
        "operations",
        [],
        ["Kiểm tra cấu hình Hermes và hàng đợi outbox."],
        "## Nhận định\nHermes không tạo được báo cáo thành phần.\n\n## Hành động đã thực hiện\nChưa có hành động tự động.\n\n## Kết quả & Tác động\nCần kiểm tra lại cấu hình Hermes.\n\n## Mức ưu tiên\nwarning");
    }

    if (!_options.FanInCompressionEnabled || partialReports.Count == 1)
    {
      return BuildFallback(partialReports);
    }

    try
    {
      var model = await llm.CompleteJsonAsync<CompressedReportModel>(
        BuildSystemPrompt(),
        BuildUserPrompt(partialReports),
        CompressionSchema,
        cancellationToken);

      return Normalize(model, partialReports);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      logger.LogWarning(ex, "Hermes fan-in compression failed; using deterministic fallback.");
      if (_options.FanInFallbackToConcatenation) return BuildFallback(partialReports);
      throw;
    }
  }

  private string BuildUserPrompt(IReadOnlyList<HermesPartialReportInput> reports)
  {
    var maxChars = Math.Clamp(_options.MaxPartialReportCharsForCompression, 1000, 20000);
    var builder = new StringBuilder();
    builder.AppendLine($"Hãy nén {reports.Count} báo cáo Hermes thành MỘT báo cáo cuối cùng cho admin.");
    builder.AppendLine("Không tạo thêm dữ kiện. Giữ bằng chứng eventId khi hữu ích. Loại bỏ trùng lặp.");
    builder.AppendLine();

    foreach (var report in reports.OrderBy(x => x.Index))
    {
      builder.AppendLine($"<partial_report index=\"{report.Index}\" severity=\"{report.Severity}\" reportType=\"{report.ReportType}\">");
      builder.AppendLine($"eventIds: {string.Join(", ", report.EventIds)}");
      builder.AppendLine("markdown:");
      builder.AppendLine(Limit(report.ReportText, maxChars));
      builder.AppendLine("</partial_report>");
      builder.AppendLine();
    }

    return builder.ToString();
  }

  private static string BuildSystemPrompt() => """
    Bạn là biên tập viên vận hành cấp cao của Áo Dài Nhã Uyên.
    Nhiệm vụ: tổng hợp nhiều báo cáo Hermes thành một báo cáo duy nhất bằng tiếng Việt.
    Output phải là JSON hợp lệ theo schema.
    Quy tắc:
    - Chỉ dùng dữ kiện trong báo cáo thành phần.
    - Không bịa endpoint, email, số liệu, GUID, hành động đã thực thi.
    - Giữ giọng văn tao nhã, chuyên nghiệp, rõ ưu tiên.
    - Markdown cuối cùng phải có các mục: ## Nhận định, ## Hành động đã thực hiện, ## Kết quả & Tác động, ## Mức ưu tiên.
    - severity chỉ dùng: info, warning, high, critical.
    - reportType chỉ dùng: growth, revenue, risk, seo, crm, operations, mixed.
    """;

  private static HermesCompressedReportResult Normalize(CompressedReportModel model, IReadOnlyList<HermesPartialReportInput> reports)
  {
    var fallback = BuildFallback(reports);
    var severity = NormalizeSeverity(model.Severity, fallback.Severity);
    var reportType = NormalizeReportType(model.ReportType, fallback.ReportType);
    var markdown = string.IsNullOrWhiteSpace(model.Markdown) ? fallback.Markdown : model.Markdown.Trim();
    var summary = string.IsNullOrWhiteSpace(model.Summary) ? Limit(markdown, 4000) : model.Summary.Trim();
    var title = string.IsNullOrWhiteSpace(model.Title) ? fallback.Title : model.Title.Trim();

    return new HermesCompressedReportResult(
      Limit(title, 200),
      Limit(summary, 4000),
      severity,
      reportType,
      model.KeyFindings?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Take(12).ToArray() ?? [],
      model.RecommendedActions?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Take(12).ToArray() ?? [],
      markdown);
  }

  private static HermesCompressedReportResult BuildFallback(IReadOnlyList<HermesPartialReportInput> reports)
  {
    var ordered = reports.OrderBy(x => x.Index).ToArray();
    var severity = ordered.Select(x => NormalizeSeverity(x.Severity, "info")).OrderByDescending(SeverityRank).FirstOrDefault() ?? "info";
    var reportType = ordered.Select(x => NormalizeReportType(x.ReportType, "mixed")).GroupBy(x => x).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "mixed";
    if (ordered.Select(x => NormalizeReportType(x.ReportType, "mixed")).Distinct().Count() > 1) reportType = "mixed";

    var builder = new StringBuilder();
    builder.AppendLine("## Nhận định");
    builder.AppendLine($"Hermes đã tạo {ordered.Length} báo cáo thành phần. Hệ thống đang dùng bản tổng hợp dự phòng.");
    builder.AppendLine();
    builder.AppendLine("## Hành động đã thực hiện");
    foreach (var report in ordered)
    {
      builder.AppendLine($"### Nhóm {report.Index + 1} ({report.EventIds.Count} sự kiện)");
      builder.AppendLine(Limit(report.ReportText, 4000));
      builder.AppendLine();
    }
    builder.AppendLine("## Kết quả & Tác động");
    builder.AppendLine("Các tín hiệu đã được gom lại để admin xử lý trong một báo cáo duy nhất.");
    builder.AppendLine();
    builder.AppendLine("## Mức ưu tiên");
    builder.AppendLine(severity);

    var markdown = builder.ToString().Trim();
    return new HermesCompressedReportResult(
      $"Báo cáo tổng hợp Hermes: {ordered.Sum(x => x.EventIds.Count)} sự kiện",
      Limit(markdown, 4000),
      severity,
      reportType,
      [],
      [],
      markdown);
  }

  private static int SeverityRank(string severity) => severity switch
  {
    "critical" => 3,
    "high" => 2,
    "warning" => 1,
    _ => 0
  };

  private static string NormalizeSeverity(string? value, string fallback)
  {
    var normalized = value?.Trim().ToLowerInvariant();
    return normalized is "info" or "warning" or "high" or "critical" ? normalized : fallback;
  }

  private static string NormalizeReportType(string? value, string fallback)
  {
    var normalized = value?.Trim().ToLowerInvariant();
    return normalized is "growth" or "revenue" or "risk" or "seo" or "crm" or "operations" or "mixed" ? normalized : fallback;
  }

  private static string Limit(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..Math.Max(0, maxLength - 1)] + "…";
  }

  private sealed record CompressedReportModel(
    string? Title,
    string? Summary,
    string? Severity,
    string? ReportType,
    IReadOnlyList<string>? KeyFindings,
    IReadOnlyList<string>? RecommendedActions,
    string? Markdown);
}
