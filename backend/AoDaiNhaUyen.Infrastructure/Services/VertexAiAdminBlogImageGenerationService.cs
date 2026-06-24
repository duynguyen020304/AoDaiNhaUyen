using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs.Admin;
using AoDaiNhaUyen.Application.Exceptions;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class VertexAiAdminBlogImageGenerationService(
  HttpClient httpClient,
  IOptions<GoogleCloudOptions> options) : IAdminBlogImageGenerationService
{
  private const string DefaultResponseMimeType = "image/png";
  private const int MaxAttempts = 3;
  private readonly GoogleCloudOptions googleCloudOptions = options.Value;

  public async Task<AdminGeneratedImageDto> GenerateAsync(
    string prompt,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(prompt))
    {
      throw new ArgumentException("Prompt tạo ảnh không được để trống.");
    }

    ValidateOptions();

    var trimmedPrompt = prompt.Trim();
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(googleCloudOptions.TimeoutSeconds, 1)));

    for (var attempt = 1; attempt <= MaxAttempts; attempt++)
    {
      using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint())
      {
        Content = JsonContent.Create(BuildPayload(trimmedPrompt))
      };
      request.Headers.Add("x-goog-api-key", googleCloudOptions.ApiKey);

      HttpResponseMessage response;
      try
      {
        response = await httpClient.SendAsync(request, timeoutCts.Token);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        throw;
      }
      catch (Exception) when (attempt < MaxAttempts)
      {
        await DelayBeforeRetryAsync(null, attempt, timeoutCts.Token);
        continue;
      }
      catch (Exception ex)
      {
        throw new AiTryOnProviderException($"Vertex AI request failed after {MaxAttempts} attempts. {ex.Message}");
      }

      using (response)
      {
        if (IsTransientStatus(response.StatusCode) && attempt < MaxAttempts)
        {
          await DelayBeforeRetryAsync(ReadRetryAfter(response), attempt, timeoutCts.Token);
          continue;
        }

        var responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
          throw new AiTryOnProviderException(
            $"Vertex AI returned {(int)response.StatusCode}. {GetProviderErrorMessage(responseBody)}");
        }

        var generatedImage = TryExtractGeneratedImage(responseBody)
          ?? throw new AiTryOnProviderException("Gemini did not return an image.");

        return new AdminGeneratedImageDto(
          Convert.FromBase64String(generatedImage.BytesBase64Encoded),
          generatedImage.MimeType,
          trimmedPrompt);
      }
    }

    throw new AiTryOnProviderException("Vertex AI request failed.");
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

  private static GeminiGenerateRequest BuildPayload(string prompt)
  {
    var safePrompt = string.Join("\n", new[]
    {
      "Create one clean, moderately detailed fashion editorial image for a Vietnamese áo dài e-commerce blog.",
      "Target production quality should be good enough for social/blog publishing, but not ultra-high-fidelity or overly heavy.",
      "Prefer simpler composition, moderate texture detail, controlled lighting, and efficient visual complexity to keep downstream media handling stable.",
      "No text overlay, no logo, no watermark, no fake claims.",
      "Elegant Vietnamese áo dài styling, boutique mood, realistic but not hyper-detailed fabric texture, soft natural or studio light.",
      prompt
    });

    return new GeminiGenerateRequest(
      [new GeminiContent("user", [GeminiPart.FromText(safePrompt)])],
      new GeminiGenerationConfig(0.6m, 32, 1m, 2048, []),
      [
        new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
        new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE")
      ]);
  }

  private static GeneratedImage? TryExtractGeneratedImage(string responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody)) return null;

    foreach (var chunk in EnumerateResponseJsonChunks(responseBody))
    {
      var image = TryExtractGeneratedImageFromChunk(chunk);
      if (image is not null) return image;
    }

    return null;
  }

  private static IEnumerable<string> EnumerateResponseJsonChunks(string responseBody)
  {
    var trimmed = responseBody.Trim();
    if (trimmed.StartsWith("[", StringComparison.Ordinal))
    {
      yield return trimmed;
      yield break;
    }

    foreach (var line in trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
      var payload = line[5..].Trim();
      if (!string.Equals(payload, "[DONE]", StringComparison.OrdinalIgnoreCase)) yield return payload;
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
        if (nestedImage is not null) return nestedImage;
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
        if (!part.TryGetProperty("inlineData", out var inlineData) ||
            !inlineData.TryGetProperty("data", out var dataElement))
        {
          continue;
        }

        var data = dataElement.GetString();
        if (string.IsNullOrWhiteSpace(data)) continue;

        var mimeType = inlineData.TryGetProperty("mimeType", out var mimeTypeElement) &&
                       !string.IsNullOrWhiteSpace(mimeTypeElement.GetString())
          ? mimeTypeElement.GetString()!
          : DefaultResponseMimeType;

        return new GeneratedImage(data, mimeType);
      }
    }

    return null;
  }

  private static bool IsTransientStatus(System.Net.HttpStatusCode statusCode)
  {
    var code = (int)statusCode;
    return code == 429 || (code >= 500 && code < 600);
  }

  private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
  {
    if (response.Headers.TryGetValues("Retry-After", out var values))
    {
      var raw = values.FirstOrDefault();
      if (raw is not null && int.TryParse(raw, out var seconds) && seconds > 0)
        return TimeSpan.FromSeconds(Math.Min(seconds, 30));
    }

    return null;
  }

  private static async Task DelayBeforeRetryAsync(TimeSpan? retryAfter, int attempt, CancellationToken ct)
  {
    var baseDelay = retryAfter ?? TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
    if (baseDelay > TimeSpan.FromSeconds(2)) baseDelay = TimeSpan.FromSeconds(2);
    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 120));
    await Task.Delay(baseDelay + jitter, ct);
  }

  private static string GetProviderErrorMessage(string responseBody)
  {
    if (string.IsNullOrWhiteSpace(responseBody)) return "No response body was returned.";

    try
    {
      using var document = JsonDocument.Parse(responseBody);
      if (TryGetProviderErrorMessage(document.RootElement, out var messageText)) return messageText;
    }
    catch (JsonException)
    {
      // Fall back to raw provider message below.
    }

    return responseBody.Length <= 500 ? responseBody : string.Concat(responseBody.AsSpan(0, 500), "...");
  }

  private static bool TryGetProviderErrorMessage(JsonElement element, out string message)
  {
    if (element.ValueKind == JsonValueKind.Array)
    {
      foreach (var item in element.EnumerateArray())
      {
        if (TryGetProviderErrorMessage(item, out message)) return true;
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
    string? Text)
  {
    public static GeminiPart FromText(string text) => new(text);
  }

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
