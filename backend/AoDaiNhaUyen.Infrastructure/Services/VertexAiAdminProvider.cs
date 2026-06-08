using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class VertexAiAdminProvider(
  HttpClient httpClient,
  IOptions<GoogleCloudOptions> options) : IAdminLlmProvider
{
  private readonly GoogleCloudOptions _config = options.Value;

  private const string SystemPrompt =
    "Bạn là trợ lý AI quản trị viên cho cửa hàng áo dài cao cấp AoDaiNhaUyen. " +
    "Nhiệm vụ của bạn:\n" +
    "- Trả lời bằng tiếng Việt, giọng chuyên nghiệp nhưng thân thiện.\n" +
    "- Giúp admin quản lý sản phẩm, danh mục, người dùng, đơn hàng.\n" +
    "- Khi đọc dữ liệu (dashboard, sản phẩm, v.v.), tóm tắt thành insight hữu ích.\n" +
    "- Khi thực hiện thay đổi, xác nhận với admin trước khi làm.\n" +
    "- Luôn minh bạch về hành động bạn đang thực hiện.\n" +
    "- Không tự ý xóa dữ liệu hoặc thay đổi role người dùng khi chưa có xác nhận.\n" +
    "- Nếu không chắc về điều gì, hãy hỏi lại admin.\n" +
    "- Quản lý đơn hàng: liệt kê, xem chi tiết, xác nhận, xử lý, giao hàng, hủy đơn.\n" +
    "- Khi có đơn hàng mới, chủ động thông báo và đề xuất xác nhận.\n" +
    "- Khi tồn kho thấp, cảnh báo admin và đề xuất nhập hàng.";

  public async IAsyncEnumerable<LlmChunk> StreamChatAsync(
    List<AdminLlmMessage> history,
    IReadOnlyList<ToolDefinition> tools,
    [EnumeratorCancellation] CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(_config.ApiKey) || string.IsNullOrWhiteSpace(_config.StylistTextModel))
    {
      yield return new LlmChunk("text", "Google Cloud AI chưa được cấu hình. Vui lòng kiểm tra biến môi trường GoogleCloud__ApiKey và GoogleCloud__StylistTextModel.");
      yield break;
    }

    var contents = BuildContents(history);
    var toolDeclarations = BuildToolDeclarations(tools);

    var payload = new GeminiStreamRequest(
      contents,
      new GeminiGenerationConfig(0.7m, 0.9m, 32, 1024),
      toolDeclarations.Count > 0
        ? [new GeminiTool(toolDeclarations)]
        : null,
      [
        new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
        new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE"),
        new GeminiSafetySetting("HARM_CATEGORY_DANGEROUS_CONTENT", "BLOCK_MEDIUM_AND_ABOVE"),
      ]);

    var endpoint = BuildStreamEndpoint();

    IReadOnlyList<LlmChunk> chunks;
    try
    {
      chunks = await SendAndReadAsync(httpClient, endpoint, payload, ct);
    }
    catch (Exception ex)
    {
      chunks = [new LlmChunk("error", $"Lỗi kết nối Google AI: {ex.Message}")];
    }

    foreach (var chunk in chunks)
      yield return chunk;
  }

  private async Task<IReadOnlyList<LlmChunk>> SendAndReadAsync(
    HttpClient httpClient,
    string endpoint,
    GeminiStreamRequest payload,
    CancellationToken ct)
  {
    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
      Content = JsonContent.Create(payload)
    };
    request.Headers.Add("x-goog-api-key", _config.ApiKey);

    using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    if (!response.IsSuccessStatusCode)
    {
      var body = await response.Content.ReadAsStringAsync(ct);
      return [new LlmChunk("error", $"Lỗi từ Google AI ({(int)response.StatusCode}): {Truncate(body, 200)}")];
    }

    await using var stream = await response.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);
    return await ReadStreamChunksAsync(reader, ct);
  }

  private static List<GeminiContent> BuildContents(List<AdminLlmMessage> history)
  {
    var contents = new List<GeminiContent>
    {
      new("user", [GeminiPart.FromText(SystemPrompt)])
    };

    foreach (var msg in history)
    {
      var role = msg.Role switch
      {
        AdminLlmRole.User => "user",
        AdminLlmRole.Assistant => "model",
        _ => "user"
      };
      contents.Add(new GeminiContent(role, [GeminiPart.FromText(msg.Content)]));
    }

    return contents;
  }

  private static List<GeminiFunctionDeclaration> BuildToolDeclarations(IReadOnlyList<ToolDefinition> tools)
  {
    var declarations = new List<GeminiFunctionDeclaration>();
    foreach (var t in tools)
    {
      var properties = new Dictionary<string, GeminiSchemaProperty>();
      if (t.Parameters.TryGetValue("properties", out var propsRaw) && propsRaw is Dictionary<string, object?> props)
      {
        foreach (var (key, val) in props)
        {
          if (val is Dictionary<string, object?> propDef)
          {
            properties[key] = new GeminiSchemaProperty(
              propDef.TryGetValue("type", out var type) ? type?.ToString() ?? "string" : "string",
              propDef.TryGetValue("description", out var desc) ? desc?.ToString() : null);
          }
        }
      }

      declarations.Add(new GeminiFunctionDeclaration(t.Name, t.Description, new GeminiFunctionParameters("object", properties)));
    }

    return declarations;
  }

  private string BuildStreamEndpoint()
  {
    var model = Uri.EscapeDataString(_config.StylistTextModel);
    if (!string.IsNullOrWhiteSpace(_config.ProjectId))
    {
      var projectId = Uri.EscapeDataString(_config.ProjectId);
      var location = Uri.EscapeDataString(_config.Location);
      return $"https://aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/publishers/google/models/{model}:streamGenerateContent?alt=sse";
    }

    return $"https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:streamGenerateContent?alt=sse";
  }

  private static async Task<List<LlmChunk>> ReadStreamChunksAsync(StreamReader reader, CancellationToken ct)
  {
    var chunks = new List<LlmChunk>();
    var textBuffer = new StringBuilder();
    string? pendingToolName = null;
    var argsBuffer = new StringBuilder();

    while (true)
    {
      var line = await reader.ReadLineAsync(ct);
      if (line is null) break;
      if (!line.StartsWith("data:")) continue;

      var json = line[5..].Trim();
      if (string.IsNullOrWhiteSpace(json) || json == "[DONE]") continue;

      try
      {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
          var candidate = candidates[0];
          if (candidate.TryGetProperty("content", out var content) &&
              content.TryGetProperty("parts", out var parts))
          {
            foreach (var part in parts.EnumerateArray())
            {
              if (part.TryGetProperty("text", out var textEl))
              {
                var text = textEl.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                  textBuffer.Append(text);
                  chunks.Add(new LlmChunk("text", text));
                }
              }

              if (part.TryGetProperty("functionCall", out var fnCall))
              {
                var fnName = fnCall.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                if (!string.IsNullOrWhiteSpace(fnName) && fnName != pendingToolName)
                {
                  if (pendingToolName is not null)
                  {
                    chunks.Add(new LlmChunk("tool_call", argsBuffer.ToString(), pendingToolName, pendingToolName));
                    argsBuffer.Clear();
                  }
                  pendingToolName = fnName;
                  argsBuffer.Clear();
                  if (fnCall.TryGetProperty("args", out var a))
                    argsBuffer.Append(a.GetRawText());
                }
                else if (fnCall.TryGetProperty("args", out var a))
                {
                  argsBuffer.Append(a.GetRawText());
                }
              }
            }
          }
        }
        else if (root.TryGetProperty("error", out var error))
        {
          var msg = error.TryGetProperty("message", out var m) ? m.GetString() ?? "Unknown error" : "Unknown error";
          chunks.Add(new LlmChunk("error", msg));
        }
      }
      catch (JsonException)
      {
        // Skip malformed SSE lines
      }
    }

    if (pendingToolName is not null && argsBuffer.Length > 0)
      chunks.Add(new LlmChunk("tool_call", argsBuffer.ToString(), pendingToolName, pendingToolName));

    if (textBuffer.Length > 0 || pendingToolName is not null)
      chunks.Add(new LlmChunk("done", "", null, null));

    return chunks;
  }

  private static string Truncate(string text, int maxLen) =>
    text.Length <= maxLen ? text : text[..maxLen] + "...";
}

// --- Gemini JSON contract types (internal) ---

internal sealed record GeminiStreamRequest(
  [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
  [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig,
  [property: JsonPropertyName("tools"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<GeminiTool>? Tools,
  [property: JsonPropertyName("safetySettings"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<GeminiSafetySetting>? SafetySettings);

internal sealed record GeminiContent(
  [property: JsonPropertyName("role")] string Role,
  [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

internal sealed record GeminiPart(
  [property: JsonPropertyName("text"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Text,
  [property: JsonPropertyName("functionResponse"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] GeminiFunctionResponseContent? FunctionResponse)
{
  public static GeminiPart FromText(string text) => new(text, null);
  public static GeminiPart FromFunctionResponse(string name, Dictionary<string, object?> content) =>
    new(null, new GeminiFunctionResponseContent(name, content));
}

internal sealed record GeminiFunctionResponseContent(
  [property: JsonPropertyName("name")] string Name,
  [property: JsonPropertyName("content")] Dictionary<string, object?> Content);

internal sealed record GeminiGenerationConfig(
  [property: JsonPropertyName("temperature")] decimal Temperature,
  [property: JsonPropertyName("topP")] decimal TopP,
  [property: JsonPropertyName("topK")] int TopK,
  [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

internal sealed record GeminiSafetySetting(
  [property: JsonPropertyName("category")] string Category,
  [property: JsonPropertyName("threshold")] string Threshold);

internal sealed record GeminiTool(
  [property: JsonPropertyName("functionDeclarations")] IReadOnlyList<GeminiFunctionDeclaration> FunctionDeclarations);

internal sealed record GeminiFunctionDeclaration(
  [property: JsonPropertyName("name")] string Name,
  [property: JsonPropertyName("description")] string Description,
  [property: JsonPropertyName("parameters")] GeminiFunctionParameters Parameters);

internal sealed record GeminiFunctionParameters(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("properties")] Dictionary<string, GeminiSchemaProperty> Properties);

internal sealed record GeminiSchemaProperty(
  [property: JsonPropertyName("type")] string Type,
  [property: JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description);
