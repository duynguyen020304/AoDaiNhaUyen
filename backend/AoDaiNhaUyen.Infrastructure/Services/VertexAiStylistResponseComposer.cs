using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class VertexAiStylistResponseComposer(
  HttpClient httpClient,
  IOptions<GoogleCloudOptions> options) : IStylistResponseComposer
{
  private readonly GoogleCloudOptions googleCloudOptions = options.Value;

  public async Task<string> ComposeAsync(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(googleCloudOptions.ApiKey) || string.IsNullOrWhiteSpace(googleCloudOptions.StylistTextModel))
    {
      return fallbackText;
    }

    var prompt = BuildPrompt(userMessage, fallbackText, intent, memorySummary, structuredPayload);
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(googleCloudOptions.TimeoutSeconds, 1)));

    try
    {
      using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint())
      {
        Content = JsonContent.Create(new GeminiTextRequest(
          [
            new GeminiContent(
              "user",
              [new GeminiPart(prompt)])
          ],
          new GeminiGenerationConfig(0.35m, 0.9m, 32, 512),
          [
            new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
            new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE")
          ]))
      };
      request.Headers.Add("x-goog-api-key", googleCloudOptions.ApiKey);

      using var response = await httpClient.SendAsync(request, timeoutCts.Token);
      if (!response.IsSuccessStatusCode)
      {
        return fallbackText;
      }

      var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
      return TryExtractText(body) ?? fallbackText;
    }
    catch
    {
      return fallbackText;
    }
  }

  public async IAsyncEnumerable<string> ComposeStreamAsync(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(googleCloudOptions.ApiKey) || string.IsNullOrWhiteSpace(googleCloudOptions.StylistTextModel))
    {
      yield return fallbackText;
      yield break;
    }

    var prompt = BuildPrompt(userMessage, fallbackText, intent, memorySummary, structuredPayload);
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(googleCloudOptions.TimeoutSeconds, 1)));

    using var response = await SendStreamRequestAsync(prompt, timeoutCts.Token, cancellationToken);
    if (response is null)
    {
      yield return fallbackText;
      yield break;
    }

    await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
    using var reader = new StreamReader(stream);

    var yieldedChunk = false;
    while (true)
    {
      var readResult = await TryReadLineAsync(reader, timeoutCts.Token, cancellationToken);
      if (readResult.Failed || readResult.Line is null)
      {
        break;
      }

      var line = readResult.Line;
      if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
      {
        continue;
      }

      var payload = line[5..].Trim();
      if (string.IsNullOrWhiteSpace(payload) || string.Equals(payload, "[DONE]", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      var delta = TryExtractText(payload, trimResult: false);
      if (string.IsNullOrWhiteSpace(delta))
      {
        continue;
      }

      yieldedChunk = true;
      yield return delta;
    }

    if (!yieldedChunk)
    {
      yield return fallbackText;
    }
  }

  private string BuildEndpoint()
  {
    var model = Uri.EscapeDataString(googleCloudOptions.StylistTextModel);
    if (!string.IsNullOrWhiteSpace(googleCloudOptions.ProjectId))
    {
      var projectId = Uri.EscapeDataString(googleCloudOptions.ProjectId);
      var location = Uri.EscapeDataString(googleCloudOptions.Location);
      return $"https://aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/publishers/google/models/{model}:generateContent";
    }

    return $"https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:generateContent";
  }

  private string BuildStreamEndpoint()
  {
    var model = Uri.EscapeDataString(googleCloudOptions.StylistTextModel);
    if (!string.IsNullOrWhiteSpace(googleCloudOptions.ProjectId))
    {
      var projectId = Uri.EscapeDataString(googleCloudOptions.ProjectId);
      var location = Uri.EscapeDataString(googleCloudOptions.Location);
      return $"https://aiplatform.googleapis.com/v1/projects/{projectId}/locations/{location}/publishers/google/models/{model}:streamGenerateContent?alt=sse";
    }

    return $"https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:streamGenerateContent?alt=sse";
  }

  private async Task<HttpResponseMessage?> SendStreamRequestAsync(
    string prompt,
    CancellationToken requestCancellationToken,
    CancellationToken callerCancellationToken)
  {
    try
    {
      using var request = new HttpRequestMessage(HttpMethod.Post, BuildStreamEndpoint())
      {
        Content = JsonContent.Create(new GeminiTextRequest(
          [
            new GeminiContent(
              "user",
              [new GeminiPart(prompt)])
          ],
          new GeminiGenerationConfig(0.35m, 0.9m, 32, 512),
          [
            new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
            new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE")
          ]))
      };
      request.Headers.Add("x-goog-api-key", googleCloudOptions.ApiKey);

      var response = await httpClient.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead,
        requestCancellationToken);
      if (response.IsSuccessStatusCode)
      {
        return response;
      }

      response.Dispose();
      return null;
    }
    catch (OperationCanceledException) when (callerCancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch
    {
      return null;
    }
  }

  private static async Task<ReadLineResult> TryReadLineAsync(
    StreamReader reader,
    CancellationToken requestCancellationToken,
    CancellationToken callerCancellationToken)
  {
    try
    {
      return new ReadLineResult(await reader.ReadLineAsync(requestCancellationToken), false);
    }
    catch (OperationCanceledException) when (callerCancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch
    {
      return new ReadLineResult(null, true);
    }
  }

  private static string BuildPrompt(
    string userMessage,
    string fallbackText,
    string intent,
    string? memorySummary,
    ChatStructuredPayloadDto? structuredPayload)
  {
    var builder = new StringBuilder();
    builder.AppendLine("Bạn là stylist AI của Ao Dai Nha Uyen.");
    builder.AppendLine("Nhiệm vụ: viết lại câu trả lời tiếng Việt tự nhiên, ngắn gọn, ấm áp nhưng chính xác.");
    builder.AppendLine("Ràng buộc:");
    builder.AppendLine("- Chỉ được dùng dữ liệu đã cung cấp.");
    builder.AppendLine("- Không được bịa sản phẩm, giá, màu, tồn kho, hay tính năng.");
    builder.AppendLine("- Nếu payload có sản phẩm, chỉ nhắc đến các sản phẩm đó.");
    builder.AppendLine("- Nếu payload yêu cầu upload ảnh hoặc có pending requirements, phải nêu rõ bước tiếp theo.");
    builder.AppendLine("- Giữ giọng văn tự nhiên như nhân viên tư vấn bán hàng giàu kinh nghiệm, thân thiện nhưng không khách sáo quá mức.");
    builder.AppendLine("- Hạn chế lạm dụng các từ quá lễ nghi hoặc lặp đi lặp lại như 'xin chào', 'dạ', 'ạ', 'rất vui được hỗ trợ'. Chỉ dùng khi thật sự hợp ngữ cảnh.");
    builder.AppendLine("- Ưu tiên trả lời trực tiếp vào nhu cầu của khách thay vì mở đầu bằng xã giao.");
    builder.AppendLine("- Nếu user đang hỏi tiếp về các sản phẩm vừa được nhắc ở lượt trước, hãy tiếp nối context đó và không quay lại hỏi discovery như dịp mặc, ngân sách, chất liệu nếu payload hoặc memory đã đủ.");
    builder.AppendLine("- Giữ phản hồi dưới 140 từ trừ khi cần liệt kê 3 gợi ý.");
    builder.AppendLine();
    builder.AppendLine($"Intent: {intent}");
    builder.AppendLine($"Memory summary: {memorySummary ?? "none"}");
    builder.AppendLine($"User message: {userMessage}");
    builder.AppendLine($"Deterministic fallback: {fallbackText}");
    builder.AppendLine("Structured payload:");
    builder.AppendLine(JsonSerializer.Serialize(structuredPayload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    builder.AppendLine();
    builder.AppendLine("Trả về chỉ văn bản trả lời cuối cùng, không markdown, không JSON.");
    return builder.ToString();
  }

  private static string? TryExtractText(string body, bool trimResult = true)
  {
    if (string.IsNullOrWhiteSpace(body))
    {
      return null;
    }

    try
    {
      using var document = JsonDocument.Parse(body);
      if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
      {
        return null;
      }

      foreach (var candidate in candidates.EnumerateArray())
      {
        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array)
        {
          continue;
        }

        var text = string.Join(
          string.Empty,
          parts.EnumerateArray()
            .Where(part => part.TryGetProperty("text", out _))
            .Select(part => part.GetProperty("text").GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(text))
        {
          return trimResult ? text.Trim() : text;
        }
      }
    }
    catch (JsonException)
    {
      return null;
    }

    return null;
  }

  private sealed record GeminiTextRequest(
    [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig,
    [property: JsonPropertyName("safetySettings")] IReadOnlyList<GeminiSafetySetting> SafetySettings);

  private sealed record GeminiContent(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

  private sealed record GeminiPart([property: JsonPropertyName("text")] string Text);

  private sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("temperature")] decimal Temperature,
    [property: JsonPropertyName("topP")] decimal TopP,
    [property: JsonPropertyName("topK")] int TopK,
    [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

  private sealed record GeminiSafetySetting(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("threshold")] string Threshold);

  private sealed record ReadLineResult(string? Line, bool Failed);
}
