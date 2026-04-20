using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class IntentClassifier(
  HttpClient httpClient,
  IOptions<GoogleCloudOptions> options) : IIntentClassifier
{
  private static readonly string[] AllowedIntents =
  [
    "catalog_lookup",
    "outfit_recommendation",
    "product_comparison",
    "tryon_prepare",
    "tryon_execute",
    "image_style_analysis",
    "clarification",
    "out_of_scope"
  ];

  private static readonly string[] AllowedScenarios = ["giao-vien", "le-tet", "du-tiec", "chup-anh"];
  private static readonly string[] AllowedColorFamilies = ["blue", "pink", "red", "ivory"];
  private static readonly string[] AllowedMaterials = ["lụa", "gấm"];
  private readonly GoogleCloudOptions googleCloudOptions = options.Value;

  public async Task<IntentClassificationDto> ClassifyAsync(
    string message,
    IReadOnlyList<ChatAttachmentDto> attachments,
    ThreadMemoryStateDto memory,
    CancellationToken cancellationToken = default)
  {
    var hasImageAttachments = attachments.Count > 0;
    var normalized = ChatTextUtils.Normalize(message);
    var fallback = IntentClassificationDto.Clarification(
      ExtractScenario(normalized) ?? memory.Scenario,
      ChatTextUtils.TryExtractBudget(message) ?? memory.BudgetCeiling,
      ExtractColorFamily(normalized) ?? memory.ColorFamily,
      ExtractMaterialKeyword(normalized) ?? memory.MaterialKeyword,
      hasImageAttachments: hasImageAttachments);

    if (string.IsNullOrWhiteSpace(message))
    {
      return fallback;
    }

    if (string.IsNullOrWhiteSpace(googleCloudOptions.ApiKey) || string.IsNullOrWhiteSpace(googleCloudOptions.StylistTextModel))
    {
      return fallback;
    }

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(googleCloudOptions.TimeoutSeconds, 1)));

    try
    {
      using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint())
      {
        Content = JsonContent.Create(new GeminiTextRequest(
          [new GeminiContent("user", [new GeminiPart(BuildPrompt(message, attachments, memory))])],
          new GeminiGenerationConfig(0.1m, 0.8m, 32, 512),
          [
            new GeminiSafetySetting("HARM_CATEGORY_HARASSMENT", "BLOCK_MEDIUM_AND_ABOVE"),
            new GeminiSafetySetting("HARM_CATEGORY_HATE_SPEECH", "BLOCK_MEDIUM_AND_ABOVE")
          ]))
      };
      request.Headers.Add("x-goog-api-key", googleCloudOptions.ApiKey);

      using var response = await httpClient.SendAsync(request, timeoutCts.Token);
      if (!response.IsSuccessStatusCode)
      {
        return fallback;
      }

      var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
      return ParseResponse(body, memory, hasImageAttachments) ?? fallback;
    }
    catch
    {
      return fallback;
    }
  }

  public static IntentClassificationDto? ParseResponse(string body, ThreadMemoryStateDto memory, bool hasImageAttachments)
  {
    var json = TryExtractText(body);
    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    try
    {
      var planner = JsonSerializer.Deserialize<PlannerOutput>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
      if (planner is null)
      {
        return null;
      }

      var intent = NormalizeAllowed(planner.Intent, AllowedIntents) ?? "clarification";
      var scenario = NormalizeAllowed(planner.Scenario, AllowedScenarios) ?? memory.Scenario;
      var colorFamily = NormalizeAllowed(planner.ColorFamily, AllowedColorFamilies) ?? memory.ColorFamily;
      var materialKeyword = NormalizeAllowed(planner.MaterialKeyword, AllowedMaterials) ?? memory.MaterialKeyword;
      var budgetCeiling = planner.BudgetCeiling > 0 ? planner.BudgetCeiling : memory.BudgetCeiling;
      var requiresPersonImage = planner.RequiresPersonImage ?? false;

      return new IntentClassificationDto(
        intent,
        scenario,
        budgetCeiling,
        colorFamily,
        materialKeyword,
        [],
        requiresPersonImage,
        hasImageAttachments);
    }
    catch (JsonException)
    {
      return null;
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

  private static string BuildPrompt(string message, IReadOnlyList<ChatAttachmentDto> attachments, ThreadMemoryStateDto memory)
  {
    var builder = new StringBuilder();
    builder.AppendLine("Bạn là bộ lập kế hoạch intent cho stylist AI Ao Dai Nha Uyen.");
    builder.AppendLine("Phân loại duy nhất một intent và trích xuất slot chuẩn hoá từ tin nhắn hiện tại, attachment và memory.");
    builder.AppendLine("Chỉ dùng các intent: catalog_lookup, outfit_recommendation, product_comparison, tryon_prepare, tryon_execute, image_style_analysis, clarification, out_of_scope.");
    builder.AppendLine("Scenario chỉ được là: giao-vien, le-tet, du-tiec, chup-anh.");
    builder.AppendLine("ColorFamily chỉ được là: blue, pink, red, ivory.");
    builder.AppendLine("MaterialKeyword chỉ được là: lụa, gấm.");
    builder.AppendLine("Chọn clarification khi thiếu dữ kiện để hành động an toàn. Chọn out_of_scope cho đơn hàng, vận chuyển, đổi trả, hoàn tiền.");
    builder.AppendLine("Chọn image_style_analysis khi người dùng gửi ảnh để xin nhận xét/gợi ý style từ ảnh. Chọn tryon_prepare hoặc tryon_execute cho nhu cầu thử đồ.");
    builder.AppendLine("requiresPersonImage=true chỉ khi yêu cầu thử đồ nhưng chưa có ảnh người mặc mới hoặc trong memory.");
    builder.AppendLine("Trả về JSON duy nhất, không markdown.");
    builder.AppendLine();
    builder.AppendLine($"Message: {message}");
    builder.AppendLine($"Attachment kinds: {(attachments.Count == 0 ? "none" : string.Join(", ", attachments.Select(item => item.Kind)))}");
    builder.AppendLine($"Memory scenario: {memory.Scenario ?? "none"}");
    builder.AppendLine($"Memory budget: {(memory.BudgetCeiling?.ToString() ?? "none")}");
    builder.AppendLine($"Memory color: {memory.ColorFamily ?? "none"}");
    builder.AppendLine($"Memory material: {memory.MaterialKeyword ?? "none"}");
    builder.AppendLine($"Memory has person image: {memory.LatestPersonAttachmentId.HasValue}");
    builder.AppendLine($"Memory selected garment product id: {(memory.SelectedGarmentProductId?.ToString() ?? "none")}");
    builder.AppendLine($"Memory shortlisted product ids: {(memory.ShortlistedProductIds.Count == 0 ? "none" : string.Join(", ", memory.ShortlistedProductIds))}");
    return builder.ToString();
  }

  private static string? NormalizeAllowed(string? value, IEnumerable<string> allowed) =>
    allowed.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));

  private static string? ExtractScenario(string normalized)
  {
    if (normalized.Contains("giao vien") || normalized.Contains("di day") || normalized.Contains("truong"))
    {
      return "giao-vien";
    }

    if (normalized.Contains("le tet") || normalized.Contains("tet") || normalized.Contains("xuan"))
    {
      return "le-tet";
    }

    if (normalized.Contains("du tiec") || normalized.Contains("su kien") || normalized.Contains("tiec"))
    {
      return "du-tiec";
    }

    if (normalized.Contains("chup anh") || normalized.Contains("len hinh"))
    {
      return "chup-anh";
    }

    return null;
  }

  private static string? ExtractColorFamily(string normalized)
  {
    if (normalized.Contains("xanh"))
    {
      return "blue";
    }

    if (normalized.Contains("hong"))
    {
      return "pink";
    }

    if (normalized.Contains("do"))
    {
      return "red";
    }

    if (normalized.Contains("trang") || normalized.Contains("kem"))
    {
      return "ivory";
    }

    return null;
  }

  private static string? ExtractMaterialKeyword(string normalized)
  {
    if (normalized.Contains("lua"))
    {
      return "lụa";
    }

    if (normalized.Contains("gam"))
    {
      return "gấm";
    }

    return null;
  }

  private static string? TryExtractText(string body)
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
          return text.Trim();
        }
      }
    }
    catch (JsonException)
    {
      return null;
    }

    return null;
  }

  private sealed record PlannerOutput(
    string? Intent,
    string? Scenario,
    decimal? BudgetCeiling,
    string? ColorFamily,
    string? MaterialKeyword,
    bool? RequiresPersonImage);

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

}
