using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class VertexAiTryOnService(
  HttpClient httpClient,
  IOptions<GoogleCloudOptions> options) : IAiTryOnService
{
  private const string DefaultResponseMimeType = "image/png";
  private readonly GoogleCloudOptions googleCloudOptions = options.Value;

  public async Task<AiTryOnResultDto> GenerateAsync(
    AiTryOnRequestDto request,
    CancellationToken cancellationToken = default)
  {
    ValidateOptions();

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(googleCloudOptions.TimeoutSeconds, 1)));

    var endpoint = BuildEndpoint();
    var payload = BuildPayload(request);

    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
      Content = JsonContent.Create(payload)
    };
    httpRequest.Headers.Add("x-goog-api-key", googleCloudOptions.ApiKey);

    using var response = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
    var responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

    if (!response.IsSuccessStatusCode)
    {
      throw new AiTryOnProviderException(
        $"Vertex AI returned {(int)response.StatusCode}. {GetProviderErrorMessage(responseBody)}");
    }

    var generatedImage = TryExtractGeneratedImage(responseBody);
    if (generatedImage is null)
    {
      throw new AiTryOnProviderException("Gemini did not return an image.");
    }

    return new AiTryOnResultDto(
      $"data:{generatedImage.MimeType};base64,{generatedImage.BytesBase64Encoded}",
      generatedImage.MimeType);
  }

  private void ValidateOptions()
  {
    if (string.IsNullOrWhiteSpace(googleCloudOptions.ApiKey))
    {
      throw new AiTryOnConfigurationException("GoogleCloud:ApiKey was not configured.");
    }

    if (string.IsNullOrWhiteSpace(googleCloudOptions.VirtualTryOnModel))
    {
      throw new AiTryOnConfigurationException("GoogleCloud:VirtualTryOnModel was not configured.");
    }
  }

  private string BuildEndpoint()
  {
    var model = Uri.EscapeDataString(googleCloudOptions.VirtualTryOnModel);
    return $"https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:streamGenerateContent";
  }

  private static GeminiGenerateRequest BuildPayload(AiTryOnRequestDto request)
  {
    var prompt = BuildTryOnPrompt(request.AccessoryImages);

    var parts = new List<GeminiPart>
    {
      GeminiPart.FromText(prompt),
      GeminiPart.FromInlineData(
        request.PersonImageMimeType,
        Convert.ToBase64String(request.PersonImageBytes)),
      GeminiPart.FromInlineData(
        request.GarmentImageMimeType,
        Convert.ToBase64String(request.GarmentImageBytes))
    };

    parts.AddRange(request.AccessoryImages.Select(accessory =>
      GeminiPart.FromInlineData(
        accessory.MimeType,
        Convert.ToBase64String(accessory.ImageBytes))));

    return new GeminiGenerateRequest(
      [
        new GeminiContent("user", parts)
      ],
      new GeminiGenerationConfig(
        0.4m,
        32,
        1m,
        2048,
        []),
      [
        new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
        new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE")
      ]);
  }

  private static string BuildTryOnPrompt(IReadOnlyList<AiTryOnAccessoryImageDto> accessories)
  {
    var accessoryNames = accessories.Count == 0
      ? "không có phụ kiện bổ sung"
      : string.Join(", ", accessories.Select(accessory =>
        string.IsNullOrWhiteSpace(accessory.DisplayName) ? accessory.Id : accessory.DisplayName.Trim()));

    return string.Join(
      "\n",
      "Hãy tạo ảnh thời trang chân thực từ các ảnh đầu vào.",
      "Ảnh 1 là ảnh người dùng/người mẫu cần thử đồ.",
      "Ảnh 2 là ảnh trang phục mẫu cần ghép lên người trong ảnh 1.",
      $"Các ảnh tiếp theo là ảnh phụ kiện đi kèm: {accessoryNames}.",
      "Yêu cầu xử lý:",
      "1. Remove background của ảnh 1.",
      "2. Lấy trang phục trong ảnh 2 và ghép cho người trong ảnh 1.",
      "3. Giữ nguyên khuôn mặt, vóc dáng, tông da và danh tính của người trong ảnh 1.",
      "4. Final image phải dùng background của ảnh 2.",
      "5. Nhân vật cuối cùng phải là người trong ảnh 1 nhưng mặc trang phục của người trong ảnh 2.",
      "6. Nếu có ảnh phụ kiện, hãy đặt phụ kiện lên nhân vật cuối cùng tự nhiên, đúng tỷ lệ và giống ảnh phụ kiện mẫu.",
      "7. Kết quả phải chân thực, toàn thân nếu có thể, ánh sáng hài hòa, không méo người, không đổi khuôn mặt.",
      "Chỉ trả về đúng một ảnh kết quả.");
  }

  private static GeneratedImage? TryExtractGeneratedImage(string responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody))
    {
      return null;
    }

    foreach (var chunk in EnumerateResponseJsonChunks(responseBody))
    {
      var image = TryExtractGeneratedImageFromChunk(chunk);
      if (image is not null)
      {
        return image;
      }
    }

    return null;
  }

  private static IEnumerable<string> EnumerateResponseJsonChunks(string responseBody)
  {
    var trimmed = responseBody.Trim();
    if (trimmed.StartsWith("["))
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

  private static GeneratedImage? TryExtractGeneratedImageFromChunk(string json)
  {
    try
    {
      using var document = JsonDocument.Parse(json);
      return TryExtractGeneratedImage(document.RootElement);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static GeneratedImage? TryExtractGeneratedImage(JsonElement root)
  {
    if (root.ValueKind == JsonValueKind.Array)
    {
      foreach (var item in root.EnumerateArray())
      {
        var nestedImage = TryExtractGeneratedImage(item);
        if (nestedImage is not null)
        {
          return nestedImage;
        }
      }

      return null;
    }

    if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
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

      foreach (var part in parts.EnumerateArray())
      {
        if (!part.TryGetProperty("inlineData", out var inlineData))
        {
          continue;
        }

        if (!inlineData.TryGetProperty("data", out var dataElement))
        {
          continue;
        }

        var data = dataElement.GetString();
        if (string.IsNullOrWhiteSpace(data))
        {
          continue;
        }

        var mimeType = inlineData.TryGetProperty("mimeType", out var mimeTypeElement) &&
                       !string.IsNullOrWhiteSpace(mimeTypeElement.GetString())
          ? mimeTypeElement.GetString()!
          : DefaultResponseMimeType;

        return new GeneratedImage(data, mimeType);
      }
    }

    return null;
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
      // Fall back to a short raw provider message below.
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

  private sealed record GeneratedImage(string BytesBase64Encoded, string MimeType);

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
    IReadOnlyList<string> StopSequences)
  {
    [JsonPropertyName("responseModalities")]
    public IReadOnlyList<string> ResponseModalities { get; init; } = ["IMAGE"];
  }

  private sealed record GeminiSafetySetting(
    [property: JsonPropertyName("category")]
    string Category,
    [property: JsonPropertyName("threshold")]
    string Threshold);
}
