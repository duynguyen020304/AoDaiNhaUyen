using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class VertexAiImageValidationService(
  HttpClient httpClient,
  IOptions<GoogleCloudOptions> options) : IImageValidationService
{
  private const string DefaultInvalidReason = "Ảnh này chưa phù hợp để thử đồ. Vui lòng gửi ảnh có người thật, rõ cơ thể và không phải logo, vật thể, động vật hoặc hình minh họa.";
  private readonly GoogleCloudOptions googleCloudOptions = options.Value;

  public async Task<ImageValidationResultDto> ValidatePersonImageAsync(
    byte[] imageBytes,
    string mimeType,
    CancellationToken cancellationToken = default)
  {
    ValidateOptions();

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(googleCloudOptions.ImageValidationTimeoutSeconds, 1)));

    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint())
    {
      Content = JsonContent.Create(BuildPayload(imageBytes, mimeType))
    };
    httpRequest.Headers.Add("x-goog-api-key", googleCloudOptions.ApiKey);

    using var response = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
    var responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

    if (!response.IsSuccessStatusCode)
    {
      throw new ImageValidationProviderException(
        $"Vertex AI image validation returned {(int)response.StatusCode}. {GetProviderErrorMessage(responseBody)}");
    }

    var text = ExtractText(responseBody);
    if (string.IsNullOrWhiteSpace(text))
    {
      throw new ImageValidationProviderException("Gemini did not return an image validation result.");
    }

    return ParseValidationResult(text);
  }

  private void ValidateOptions()
  {
    if (string.IsNullOrWhiteSpace(googleCloudOptions.ApiKey))
    {
      throw new ImageValidationConfigurationException("GoogleCloud:ApiKey was not configured.");
    }

    if (string.IsNullOrWhiteSpace(googleCloudOptions.ImageValidationModel))
    {
      throw new ImageValidationConfigurationException("GoogleCloud:ImageValidationModel was not configured.");
    }
  }

  private string BuildEndpoint()
  {
    var model = Uri.EscapeDataString(googleCloudOptions.ImageValidationModel);
    return $"https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:streamGenerateContent";
  }

  private static GeminiGenerateRequest BuildPayload(byte[] imageBytes, string mimeType)
  {
    return new GeminiGenerateRequest(
      [
        new GeminiContent(
          "user",
          [
            GeminiPart.FromText(BuildPrompt()),
            GeminiPart.FromInlineData(mimeType, Convert.ToBase64String(imageBytes))
          ])
      ],
      new GeminiGenerationConfig(0m, 16, 1m, 256, [], "application/json"),
      [
        new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
        new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE")
      ]);
  }

  private static string BuildPrompt()
  {
    return string.Join(
      "\n",
      "Bạn là bộ kiểm tra ảnh đầu vào cho tính năng thử đồ ảo.",
      "Ảnh này sẽ được dùng làm ảnh người/người mẫu để ghép trang phục.",
      "Chấp nhận ảnh có một người thật với khuôn mặt rõ ràng, hợp lý để nhận diện người dùng, kể cả ảnh chân dung chỉ thấy đầu và vai.",
      "Không yêu cầu thấy toàn thân hoặc thấy đủ cơ thể. Ảnh chân dung, ảnh thẻ, selfie rõ mặt đều hợp lệ.",
      "Từ chối nếu ảnh là logo, hình minh họa, meme, screenshot, động vật, đồ vật, sách, bút, sản phẩm đơn lẻ, không có người thật, nhiều người nổi bật, mặt bị che quá nhiều, ảnh quá mờ hoặc nội dung không phù hợp.",
      "Nếu thấy rõ một khuôn mặt người thật và ảnh không thuộc nhóm cần từ chối, hãy trả về isValid=true.",
      "Trả về JSON duy nhất, không markdown, theo schema:",
      "{\"isValid\":true,\"category\":\"valid_person_face\",\"reason\":\"Ảnh có khuôn mặt người rõ ràng, phù hợp để thử đồ.\",\"confidence\":0.95}");
  }

  private static ImageValidationResultDto ParseValidationResult(string responseText)
  {
    var json = StripJsonFence(responseText);

    try
    {
      using var document = JsonDocument.Parse(json);
      var root = document.RootElement;

      if (!root.TryGetProperty("isValid", out var isValidElement) ||
          (isValidElement.ValueKind != JsonValueKind.True && isValidElement.ValueKind != JsonValueKind.False))
      {
        throw new ImageValidationProviderException("Gemini image validation result did not include isValid.");
      }

      var isValid = isValidElement.GetBoolean();
      var reason = root.TryGetProperty("reason", out var reasonElement) &&
                   !string.IsNullOrWhiteSpace(reasonElement.GetString())
        ? reasonElement.GetString()!.Trim()
        : DefaultInvalidReason;
      var category = root.TryGetProperty("category", out var categoryElement) &&
                     !string.IsNullOrWhiteSpace(categoryElement.GetString())
        ? categoryElement.GetString()!.Trim()
        : null;
      var confidence = root.TryGetProperty("confidence", out var confidenceElement) &&
                       confidenceElement.ValueKind == JsonValueKind.Number &&
                       confidenceElement.TryGetDecimal(out var confidenceValue)
        ? confidenceValue
        : (decimal?)null;

      return new ImageValidationResultDto(isValid, reason, category, confidence);
    }
    catch (JsonException exception)
    {
      throw new ImageValidationProviderException($"Gemini image validation result was not valid JSON. {exception.Message}");
    }
  }

  private static string StripJsonFence(string text)
  {
    var trimmed = text.Trim();
    if (!trimmed.StartsWith("```", StringComparison.Ordinal))
    {
      return trimmed;
    }

    var firstNewLine = trimmed.IndexOf('\n');
    var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
    if (firstNewLine < 0 || lastFence <= firstNewLine)
    {
      return trimmed;
    }

    return trimmed[(firstNewLine + 1)..lastFence].Trim();
  }

  private static string? ExtractText(string responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody))
    {
      return null;
    }

    var textParts = new List<string>();
    foreach (var chunk in EnumerateResponseJsonChunks(responseBody))
    {
      ExtractTextFromChunk(chunk, textParts);
    }

    return textParts.Count == 0 ? null : string.Concat(textParts);
  }

  private static IEnumerable<string> EnumerateResponseJsonChunks(string responseBody)
  {
    var trimmed = responseBody.Trim();
    if (trimmed.StartsWith("[", StringComparison.Ordinal) || trimmed.StartsWith("{", StringComparison.Ordinal))
    {
      yield return trimmed;
      yield break;
    }

    foreach (var line in trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
      {
        var payload = line[5..].Trim();
        if (!string.Equals(payload, "[DONE]", StringComparison.OrdinalIgnoreCase))
        {
          yield return payload;
        }
      }
    }
  }

  private static void ExtractTextFromChunk(string json, List<string> textParts)
  {
    try
    {
      using var document = JsonDocument.Parse(json);
      ExtractText(document.RootElement, textParts);
    }
    catch (JsonException)
    {
    }
  }

  private static void ExtractText(JsonElement root, List<string> textParts)
  {
    if (root.ValueKind == JsonValueKind.Array)
    {
      foreach (var item in root.EnumerateArray())
      {
        ExtractText(item, textParts);
      }

      return;
    }

    if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
    {
      return;
    }

    foreach (var candidate in candidates.EnumerateArray())
    {
      if (!candidate.TryGetProperty("content", out var content) ||
          !content.TryGetProperty("parts", out var parts) ||
          parts.ValueKind != JsonValueKind.Array)
      {
        continue;
      }

      foreach (var part in parts.EnumerateArray())
      {
        if (part.TryGetProperty("text", out var textElement) && !string.IsNullOrWhiteSpace(textElement.GetString()))
        {
          textParts.Add(textElement.GetString()!);
        }
      }
    }
  }

  private static string GetProviderErrorMessage(string responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody))
    {
      return "No response body was returned.";
    }

    try
    {
      using var document = JsonDocument.Parse(responseBody);
      if (TryGetProviderErrorMessage(document.RootElement, out var messageText))
      {
        return messageText;
      }
    }
    catch (JsonException)
    {
    }

    return responseBody.Length <= 500
      ? responseBody
      : string.Concat(responseBody.AsSpan(0, 500), "...");
  }

  private static bool TryGetProviderErrorMessage(JsonElement element, out string message)
  {
    if (element.ValueKind == JsonValueKind.Array)
    {
      foreach (var item in element.EnumerateArray())
      {
        if (TryGetProviderErrorMessage(item, out message))
        {
          return true;
        }
      }
    }
    else if (element.ValueKind == JsonValueKind.Object &&
             element.TryGetProperty("error", out var error) &&
             error.ValueKind == JsonValueKind.Object &&
             error.TryGetProperty("message", out var messageElement))
    {
      message = messageElement.GetString() ?? "No error message was returned.";
      return true;
    }

    message = string.Empty;
    return false;
  }

  private sealed record GeminiGenerateRequest(
    [property: JsonPropertyName("contents")]
    IReadOnlyList<GeminiContent> Contents,
    [property: JsonPropertyName("generationConfig")]
    GeminiGenerationConfig GenerationConfig,
    [property: JsonPropertyName("safetySettings")]
    IReadOnlyList<GeminiSafetySetting> SafetySettings);

  private sealed record GeminiContent(
    [property: JsonPropertyName("role")]
    string Role,
    [property: JsonPropertyName("parts")]
    IReadOnlyList<GeminiPart> Parts);

  private sealed record GeminiPart(
    [property: JsonPropertyName("text")]
    string? Text,
    [property: JsonPropertyName("inlineData")]
    GeminiInlineData? InlineData)
  {
    public static GeminiPart FromText(string text) => new(text, null);

    public static GeminiPart FromInlineData(string mimeType, string data) =>
      new(null, new GeminiInlineData(mimeType, data));
  }

  private sealed record GeminiInlineData(
    [property: JsonPropertyName("mimeType")]
    string MimeType,
    [property: JsonPropertyName("data")]
    string Data);

  private sealed record GeminiGenerationConfig(
    [property: JsonPropertyName("temperature")]
    decimal Temperature,
    [property: JsonPropertyName("topK")]
    int TopK,
    [property: JsonPropertyName("topP")]
    decimal TopP,
    [property: JsonPropertyName("maxOutputTokens")]
    int MaxOutputTokens,
    [property: JsonPropertyName("stopSequences")]
    IReadOnlyList<string> StopSequences,
    [property: JsonPropertyName("responseMimeType")]
    string ResponseMimeType);

  private sealed record GeminiSafetySetting(
    [property: JsonPropertyName("category")]
    string Category,
    [property: JsonPropertyName("threshold")]
    string Threshold);
}
