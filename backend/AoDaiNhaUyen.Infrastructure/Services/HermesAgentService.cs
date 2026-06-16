using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class HermesAgentService(
  IHttpClientFactory httpClientFactory,
  IOptions<HermesAgentOptions> options,
  ILogger<HermesAgentService> logger) : IHermesAgentService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly ConcurrentQueue<HermesRunRecord> Runs = new();
  private static HermesHeartbeatSnapshot? _lastHeartbeat;

  private readonly HermesAgentOptions _options = options.Value;

  public Task<HermesStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
  {
    var heartbeat = _lastHeartbeat;
    var now = DateTimeOffset.UtcNow;
    var status = heartbeat is null
      ? "offline"
      : now - heartbeat.CreatedAt > TimeSpan.FromMinutes(5)
        ? "stale"
        : heartbeat.Status;

    return Task.FromResult(new HermesStatusResponse(
      status,
      heartbeat?.RunnerName ?? _options.RunnerName,
      heartbeat?.CreatedAt,
      heartbeat?.Model,
      heartbeat?.GatewayStatus,
      heartbeat?.ActiveJobs ?? 0,
      heartbeat?.LastError,
      IsApiConfigured()));
  }

  public Task RecordHeartbeatAsync(HermesHeartbeatRequest request, CancellationToken cancellationToken)
  {
    _lastHeartbeat = new HermesHeartbeatSnapshot(
      request.RunnerName.Trim(),
      NormalizeStatus(request.Status),
      request.Model,
      request.GatewayStatus,
      Math.Max(0, request.ActiveJobs),
      request.LastError,
      DateTimeOffset.UtcNow);

    return Task.CompletedTask;
  }

  public async IAsyncEnumerable<HermesStreamChunk> StreamChatAsync(
    HermesChatRequest request,
    Guid adminUserId,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var run = new HermesRunRecord(
      Guid.NewGuid(),
      "running",
      "admin_chat",
      request.Message,
      null,
      DateTimeOffset.UtcNow,
      null,
      null);
    AddRun(run);

    yield return new HermesStreamChunk("conversation", request.ConversationId ?? run.Id.ToString("N"));
    yield return new HermesStreamChunk("tool_call", "Đang gửi yêu cầu tới Hermes Agent…", "hermes_api", run.Id.ToString("N"));

    if (!IsApiConfigured())
    {
      var message = "Hermes API server chưa cấu hình. Cần Hermes__ApiServerUrl và Hermes__ApiServerKey.";
      CompleteRun(run.Id, "failed", null, message);
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
      throw;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "[HermesAgent] Chat call failed. RunId={RunId}", run.Id);
      const string message = "Không gọi được Hermes Agent. Kiểm tra gateway/API server trên VPS.";
      CompleteRun(run.Id, "failed", null, ex.Message);
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

    CompleteRun(run.Id, "completed", text, null);
    yield return new HermesStreamChunk("text", text);
  }

  public Task<IReadOnlyList<HermesRunSummaryResponse>> ListRunsAsync(CancellationToken cancellationToken)
  {
    IReadOnlyList<HermesRunSummaryResponse> runs = Runs
      .Reverse()
      .Take(50)
      .Select(r => new HermesRunSummaryResponse(
        r.Id,
        r.Status,
        r.Trigger,
        Truncate(r.Prompt, 120),
        Truncate(r.Result, 160),
        r.StartedAt,
        r.CompletedAt,
        r.Error))
      .ToList();

    return Task.FromResult(runs);
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
      instructions = BuildInstructions(),
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
      throw new InvalidOperationException($"Hermes API returned {(int)response.StatusCode}: {Truncate(body, 500)}");
    }

    return JsonSerializer.Deserialize<HermesResponse>(body, JsonOptions);
  }

  private static string BuildInstructions() =>
    "Bạn là Hermes Agent quản trị cho AoDaiNhaUyen. Trả lời tiếng Việt. " +
    "Ưu tiên đọc/kiểm tra an toàn. Không thực hiện thay đổi phá hủy nếu chưa có phê duyệt rõ ràng. " +
    "Nếu cần thao tác admin, dùng API nội bộ và mô tả rõ rủi ro.";

  private bool IsApiConfigured() =>
    Uri.TryCreate(_options.ApiServerUrl, UriKind.Absolute, out _) &&
    !string.IsNullOrWhiteSpace(_options.ApiServerKey);

  private static IEnumerable<HermesStreamChunk> ExtractToolEvents(HermesResponse? response)
  {
    foreach (var output in response?.Output ?? [])
    {
      if (output.Type == "function_call")
      {
        yield return new HermesStreamChunk(
          "tool_call",
          output.Arguments ?? string.Empty,
          output.Name ?? "hermes_tool",
          output.CallId);
      }
      else if (output.Type == "function_call_output")
      {
        yield return new HermesStreamChunk(
          "tool_result",
          output.Output ?? string.Empty,
          "hermes_tool",
          output.CallId);
      }
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

  private static string NormalizeStatus(string status) =>
    string.IsNullOrWhiteSpace(status) ? "unknown" : status.Trim().ToLowerInvariant();

  private static string Truncate(string? text, int max) =>
    string.IsNullOrEmpty(text) || text.Length <= max ? text ?? string.Empty : text[..max] + "…";

  private static void AddRun(HermesRunRecord run)
  {
    Runs.Enqueue(run);
    while (Runs.Count > 100 && Runs.TryDequeue(out _)) { }
  }

  private static void CompleteRun(Guid id, string status, string? result, string? error)
  {
    var snapshot = Runs.ToArray();
    Runs.Clear();
    foreach (var run in snapshot)
    {
      Runs.Enqueue(run.Id == id
        ? run with { Status = status, Result = result, CompletedAt = DateTimeOffset.UtcNow, Error = error }
        : run);
    }
  }

  private sealed record HermesHeartbeatSnapshot(
    string RunnerName,
    string Status,
    string? Model,
    string? GatewayStatus,
    int ActiveJobs,
    string? LastError,
    DateTimeOffset CreatedAt);

  private sealed record HermesRunRecord(
    Guid Id,
    string Status,
    string Trigger,
    string Prompt,
    string? Result,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? Error);

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
