using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using AoDaiNhaUyen.Domain.Entities;
using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class HermesAgentService(
  IHttpClientFactory httpClientFactory,
  IOptions<HermesAgentOptions> options,
  AppDbContext dbContext,
  ILogger<HermesAgentService> logger) : IHermesAgentService
{
  private const int MaxReportPageSize = 100;
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly HashSet<string> AllowedSeverities = new(StringComparer.OrdinalIgnoreCase)
  {
    "info", "warning", "high", "critical"
  };

  private readonly HermesAgentOptions _options = options.Value;

  public async Task<HermesStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
  {
    var heartbeat = await dbContext.HermesHeartbeats
      .AsNoTracking()
      .OrderByDescending(x => x.RecordedAt)
      .FirstOrDefaultAsync(cancellationToken);

    var now = DateTimeOffset.UtcNow;
    var status = heartbeat is null
      ? "offline"
      : now - heartbeat.RecordedAt > TimeSpan.FromMinutes(5)
        ? "stale"
        : heartbeat.Status;

    return new HermesStatusResponse(
      status,
      heartbeat?.RunnerName ?? _options.RunnerName,
      heartbeat?.RecordedAt,
      heartbeat?.Model,
      heartbeat?.GatewayStatus,
      heartbeat?.ActiveJobs ?? 0,
      heartbeat?.LastError,
      IsApiConfigured());
  }

  public async Task RecordHeartbeatAsync(HermesHeartbeatRequest request, CancellationToken cancellationToken)
  {
    var now = DateTimeOffset.UtcNow;
    dbContext.HermesHeartbeats.Add(new HermesHeartbeat
    {
      Id = Guid.NewGuid(),
      RunnerName = LimitRequired(request.RunnerName, 120),
      Status = NormalizeStatus(request.Status),
      Model = Limit(request.Model, 160),
      GatewayStatus = Limit(request.GatewayStatus, 120),
      ActiveJobs = Math.Max(0, request.ActiveJobs),
      LastError = NormalizeOptionalText(request.LastError),
      RecordedAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    });

    await dbContext.SaveChangesAsync(cancellationToken);
  }

  public async IAsyncEnumerable<HermesStreamChunk> StreamChatAsync(
    HermesChatRequest request,
    Guid adminUserId,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var now = DateTimeOffset.UtcNow;
    var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
      ? Guid.NewGuid().ToString("N")
      : request.ConversationId.Trim();

    var run = new HermesRun
    {
      Id = Guid.NewGuid(),
      Status = "running",
      Trigger = "admin_chat",
      AdminUserId = adminUserId,
      ConversationId = Limit(conversationId, 160),
      PromptPreview = NormalizeRequiredText(request.Message),
      StartedAt = now,
      CreatedAt = now.UtcDateTime,
      UpdatedAt = now.UtcDateTime
    };

    dbContext.HermesRuns.Add(run);
    await dbContext.SaveChangesAsync(cancellationToken);

    yield return new HermesStreamChunk("conversation", conversationId);

    if (!IsApiConfigured())
    {
      var message = "Hermes API server chưa cấu hình. Cần Hermes__ApiServerUrl và Hermes__ApiServerKey.";
      await CompleteRunAsync(run.Id, "failed", null, message, cancellationToken);
      yield return new HermesStreamChunk("error", message);
      yield break;
    }

    HermesResponse? hermesResponse;
    HermesStreamChunk? errorChunk = null;
    try
    {
      hermesResponse = await CallHermesResponsesApiAsync(request, cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      await CompleteRunAsync(run.Id, "cancelled", null, "Client disconnected.", CancellationToken.None);
      throw;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "[HermesAgent] Chat call failed. RunId={RunId}", run.Id);
      const string message = "Không gọi được Hermes Agent. Kiểm tra gateway/API server trên VPS.";
      await CompleteRunAsync(run.Id, "failed", null, ex.Message, cancellationToken);
      hermesResponse = null;
      errorChunk = new HermesStreamChunk("error", message);
    }

    if (errorChunk is not null)
    {
      yield return errorChunk;
      yield break;
    }

    var toolEvents = ExtractToolEvents(hermesResponse).ToList();
    foreach (var toolEvent in toolEvents)
    {
      yield return toolEvent;
    }

    var text = ExtractAssistantText(hermesResponse);
    if (string.IsNullOrWhiteSpace(text))
    {
      text = "Hermes Agent đã phản hồi nhưng không có nội dung văn bản.";
    }

    await CompleteRunAsync(run.Id, "completed", text, null, cancellationToken);
    yield return new HermesStreamChunk("text", text);
  }

  public async Task<IReadOnlyList<HermesRunSummaryResponse>> ListRunsAsync(CancellationToken cancellationToken)
  {
    var runs = await dbContext.HermesRuns
      .AsNoTracking()
      .OrderByDescending(x => x.StartedAt)
      .Take(50)
      .Select(x => new
      {
        x.Id,
        x.Status,
        x.Trigger,
        x.PromptPreview,
        x.ResultPreview,
        x.StartedAt,
        x.CompletedAt,
        x.Error
      })
      .ToListAsync(cancellationToken);

    return runs
      .Select(x => new HermesRunSummaryResponse(
        x.Id,
        x.Status,
        x.Trigger,
        Truncate(x.PromptPreview, 120),
        Truncate(x.ResultPreview, 160),
        x.StartedAt,
        x.CompletedAt,
        x.Error))
      .ToList();
  }

  public async Task<HermesReportResponse> RecordReportAsync(
    HermesReportRequest request,
    CancellationToken cancellationToken)
  {
    var payloadJson = NormalizePayloadJson(request.PayloadJson);
    if (request.RunId is not null && !await dbContext.HermesRuns.AnyAsync(x => x.Id == request.RunId, cancellationToken))
    {
      throw new ArgumentException("RunId không tồn tại.", nameof(request.RunId));
    }

    var now = DateTime.UtcNow;
    var report = new HermesReport
    {
      Id = Guid.NewGuid(),
      ReportType = LimitRequired(request.ReportType, 80),
      Severity = NormalizeSeverity(request.Severity),
      Title = LimitRequired(request.Title, 200),
      Summary = NormalizeRequiredText(request.Summary),
      PayloadJson = payloadJson,
      Source = Limit(request.Source, 80) ?? "hermes_agent",
      CorrelationId = Limit(request.CorrelationId, 128),
      RunId = request.RunId,
      Status = "open",
      CreatedAt = now,
      UpdatedAt = now
    };

    dbContext.HermesReports.Add(report);
    await dbContext.SaveChangesAsync(cancellationToken);
    return MapReport(report);
  }

  public async Task<PagedResult<HermesReportListItemResponse>> ListReportsAsync(
    HermesReportSearchRequest request,
    CancellationToken cancellationToken)
  {
    var page = Math.Max(1, request.Page);
    var pageSize = Math.Clamp(request.PageSize, 1, MaxReportPageSize);
    var query = dbContext.HermesReports.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(request.Severity))
    {
      var severity = NormalizeSeverity(request.Severity);
      query = query.Where(x => x.Severity == severity);
    }

    if (!string.IsNullOrWhiteSpace(request.Type))
    {
      var type = request.Type.Trim();
      query = query.Where(x => x.ReportType == type);
    }

    if (!string.IsNullOrWhiteSpace(request.Status))
    {
      var status = LimitRequired(request.Status, 40).ToLowerInvariant();
      query = query.Where(x => x.Status == status);
    }

    if (!string.IsNullOrWhiteSpace(request.Source))
    {
      var source = LimitRequired(request.Source, 80);
      query = query.Where(x => x.Source == source);
    }

    if (!string.IsNullOrWhiteSpace(request.Q))
    {
      var q = request.Q.Trim();
      query = query.Where(x => x.Title.Contains(q) || x.Summary.Contains(q) || (x.CorrelationId != null && x.CorrelationId.Contains(q)));
    }

    var total = await query.CountAsync(cancellationToken);
    var rows = await query
      .OrderByDescending(x => x.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(x => new
      {
        x.Id,
        x.ReportType,
        x.Severity,
        x.Title,
        x.Summary,
        x.Source,
        x.CorrelationId,
        x.RunId,
        x.Status,
        x.CreatedAt
      })
      .ToListAsync(cancellationToken);

    var items = rows
      .Select(x => new HermesReportListItemResponse(
        x.Id,
        x.ReportType,
        x.Severity,
        x.Title,
        Truncate(x.Summary, 180),
        x.Source,
        x.CorrelationId,
        x.RunId,
        x.Status,
        x.CreatedAt))
      .ToList();

    return new PagedResult<HermesReportListItemResponse>(items, total, page, pageSize);
  }

  public async Task<HermesReportResponse?> GetReportAsync(Guid id, CancellationToken cancellationToken)
  {
    return await dbContext.HermesReports
      .AsNoTracking()
      .Where(x => x.Id == id)
      .Select(x => new HermesReportResponse(
        x.Id,
        x.ReportType,
        x.Severity,
        x.Title,
        x.Summary,
        x.PayloadJson,
        x.Source,
        x.CorrelationId,
        x.RunId,
        x.Status,
        x.CreatedAt))
      .FirstOrDefaultAsync(cancellationToken);
  }

  private async Task CompleteRunAsync(Guid id, string status, string? result, string? error, CancellationToken cancellationToken)
  {
    var run = await dbContext.HermesRuns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (run is null) return;

    run.Status = status;
    run.ResultPreview = NormalizeOptionalText(result);
    run.Error = NormalizeOptionalText(error);
    run.CompletedAt = DateTimeOffset.UtcNow;
    run.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<HermesResponse?> CallHermesResponsesApiAsync(
    HermesChatRequest request,
    CancellationToken cancellationToken)
  {
    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromMinutes(3);
    client.BaseAddress = new Uri(_options.ApiServerUrl!, UriKind.Absolute);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiServerKey);

    var payload = new
    {
      model = "hermes-agent",
      input = request.Message,
      store = true,
      conversation = string.IsNullOrWhiteSpace(request.ConversationId)
        ? "aodai-admin-hermes"
        : $"aodai-admin-hermes-{request.ConversationId}"
    };

    using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
    using var response = await client.PostAsync("/v1/responses", content, cancellationToken);
    var body = await response.Content.ReadAsStringAsync(cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      throw new InvalidOperationException($"Hermes API returned {(int)response.StatusCode}: {body}");
    }

    return JsonSerializer.Deserialize<HermesResponse>(body, JsonOptions);
  }

  private bool IsApiConfigured() =>
    Uri.TryCreate(_options.ApiServerUrl, UriKind.Absolute, out _) &&
    !string.IsNullOrWhiteSpace(_options.ApiServerKey);

  private static IEnumerable<HermesStreamChunk> ExtractToolEvents(HermesResponse? response)
  {
    foreach (var output in response?.Output ?? [])
    {
      if (output.Type == "function_call")
      {
        // Suppress raw tool_call arguments — only emit a friendly label for report creation
        if (output.Name is not null && output.Name.Contains("/api/admin/hermes/report", StringComparison.OrdinalIgnoreCase))
        {
          yield return new HermesStreamChunk(
            "tool_call",
            "Đang ghi báo cáo phân tích…",
            output.Name,
            output.CallId);
        }
      }
      // Skip function_call_output entirely — raw JSON/curl-like output is not user-facing
    }
  }

  private static string ExtractAssistantText(HermesResponse? response)
  {
    if (response?.Output is null) return string.Empty;

    var builder = new StringBuilder();
    foreach (var output in response.Output)
    {
      if (output.Type != "message" || output.Content is null) continue;
      foreach (var part in output.Content)
      {
        if (!string.IsNullOrWhiteSpace(part.Text))
        {
          if (builder.Length > 0) builder.AppendLine();
          builder.Append(part.Text);
        }
      }
    }

    return builder.ToString();
  }

  private static HermesReportResponse MapReport(HermesReport report) =>
    new(
      report.Id,
      report.ReportType,
      report.Severity,
      report.Title,
      report.Summary,
      report.PayloadJson,
      report.Source,
      report.CorrelationId,
      report.RunId,
      report.Status,
      report.CreatedAt);

  private static string? NormalizePayloadJson(string? payloadJson)
  {
    if (string.IsNullOrWhiteSpace(payloadJson)) return null;
    var trimmed = payloadJson.Trim();
    using var _ = JsonDocument.Parse(trimmed);
    return trimmed;
  }

  private static string NormalizeSeverity(string severity)
  {
    var value = LimitRequired(severity, 30).ToLowerInvariant();
    if (!AllowedSeverities.Contains(value)) throw new ArgumentException("Mức độ báo cáo Hermes không hợp lệ.", nameof(severity));
    return value;
  }

  private static string NormalizeStatus(string status) =>
    string.IsNullOrWhiteSpace(status) ? "unknown" : LimitRequired(status, 80).ToLowerInvariant();

  private static string NormalizeRequiredText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Thiếu dữ liệu bắt buộc.", nameof(value));
    return value.Trim();
  }

  private static string? NormalizeOptionalText(string? value)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    return value.Trim();
  }

  private static string LimitRequired(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Thiếu dữ liệu bắt buộc.", nameof(value));
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }

  private static string? Limit(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
  }

  private static string Truncate(string? text, int max) =>
    string.IsNullOrEmpty(text) || text.Length <= max ? text ?? string.Empty : text[..Math.Max(0, max - 1)] + "…";

  private sealed record HermesResponse(HermesOutput[]? Output);

  private sealed record HermesOutput(
    string? Type,
    string? Name,
    string? Arguments,
    string? CallId,
    string? Output,
    HermesContentPart[]? Content);

  private sealed record HermesContentPart(string? Type, string? Text);
}
