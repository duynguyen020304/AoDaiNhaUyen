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
    "accessory_recommendation",
    "product_description",
    "product_comparison",
    "tryon_prepare",
    "tryon_execute",
    "image_style_analysis",
    "clarification",
    "out_of_scope"
  ];

  private static readonly string[] AllowedScenarios = ["giao-vien", "le-tet", "du-tiec", "chup-anh"];
  private static readonly string[] AllowedColorFamilies = ["blue", "pink", "red", "ivory", "black", "purple", "green", "yellow", "brown", "gold", "white"];
  private static readonly string[] AllowedMaterials = ["lụa", "gấm"];
  private readonly GoogleCloudOptions googleCloudOptions = options.Value;

  public async Task<IntentClassificationDto> ClassifyAsync(
    string message,
    IReadOnlyList<ChatAttachmentDto> attachments,
    ThreadMemoryStateDto memory,
    string? previousUserMessage = null,
    string? previousAssistantMessage = null,
    CancellationToken cancellationToken = default)
  {
    var hasImageAttachments = attachments.Count > 0;
    var normalized = ChatTextUtils.Normalize(message);
    var fallback = BuildPromptDrivenFallback(message, normalized, memory, hasImageAttachments);

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
          [new GeminiContent("user", [new GeminiPart(BuildPrompt(message, attachments, memory, previousUserMessage, previousAssistantMessage))])],
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
      var parsed = ParseResponse(body, memory, hasImageAttachments);
      return parsed is null
        ? fallback
        : EnrichPromptClassification(parsed, message, normalized, memory, hasImageAttachments);
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

    var plannerJson = ExtractFirstBalancedJsonObject(json);
    if (string.IsNullOrWhiteSpace(plannerJson))
    {
      return null;
    }

    try
    {
      var planner = JsonSerializer.Deserialize<PlannerOutput>(plannerJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
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
      var responseMode = NormalizeAllowed(planner.ResponseMode, ["direct_answer", "recommendation", "comparison", "clarification", "tryon", "image_analysis"]);
      var selectionStrategy = NormalizeAllowed(planner.SelectionStrategy, ["relevance_first", "novelty_first", "stylist_pick", "comparison_focus", "image_led"]);
      var productReferenceScope = NormalizeAllowed(planner.ProductReferenceScope, ["none", "selected_garment", "shortlist_all", "shortlist_top_3"]);

      return new IntentClassificationDto(
        intent,
        scenario,
        budgetCeiling,
        colorFamily,
        materialKeyword,
        NormalizeAllowed(planner.ProductType, ["ao_dai", "phu_kien"]),
        [],
        requiresPersonImage,
        hasImageAttachments,
        responseMode,
        planner.NeedsCatalogLookup ?? intent is "catalog_lookup" or "outfit_recommendation" or "accessory_recommendation" or "product_description" or "product_comparison",
        planner.NeedsClarification ?? intent == "clarification",
        string.IsNullOrWhiteSpace(planner.RetrievalQuery) ? null : planner.RetrievalQuery.Trim(),
        selectionStrategy,
        string.IsNullOrWhiteSpace(planner.StylistBrief) ? null : planner.StylistBrief.Trim(),
        NormalizeReferencedImageHint(planner.ReferencedImageHint),
        productReferenceScope == "none" ? null : productReferenceScope,
        planner.WantsDifferentOptions ?? string.Equals(selectionStrategy, "novelty_first", StringComparison.OrdinalIgnoreCase),
        planner.HasSpecificAccessoryRequest ?? false);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  public static IntentClassificationDto BuildPromptDrivenFallback(
    string message,
    string normalized,
    ThreadMemoryStateDto memory,
    bool hasImageAttachments)
  {
    var fallback = BuildDegradedFallbackClassification(message, normalized, memory, hasImageAttachments);
    return EnrichPromptClassification(fallback, message, normalized, memory, hasImageAttachments);
  }

  private static IntentClassificationDto EnrichPromptClassification(
    IntentClassificationDto classification,
    string message,
    string normalized,
    ThreadMemoryStateDto memory,
    bool hasImageAttachments)
  {
    var responseMode = classification.Intent switch
    {
      "product_comparison" => "comparison",
      "tryon_prepare" or "tryon_execute" => "tryon",
      "image_style_analysis" => "image_analysis",
      "outfit_recommendation" or "accessory_recommendation" => "recommendation",
      "clarification" => "clarification",
      _ => "direct_answer"
    };

    var retrievalQuery = classification.NeedsCatalogLookup == false && classification.Intent is not "catalog_lookup" and not "outfit_recommendation" and not "accessory_recommendation" and not "product_description" and not "product_comparison"
      ? classification.RetrievalQuery
      : BuildRetrievalQuery(message, classification);
    var selectionStrategy = !string.IsNullOrWhiteSpace(classification.SelectionStrategy)
      ? classification.SelectionStrategy
      : classification.Intent switch
      {
        "product_comparison" => "comparison_focus",
        "image_style_analysis" => "image_led",
        "outfit_recommendation" or "accessory_recommendation" => "stylist_pick",
        _ => classification.Intent is "catalog_lookup" or "product_description"
          ? "relevance_first"
          : null
      };

    return classification with
    {
      ResponseMode = classification.ResponseMode ?? responseMode,
      NeedsCatalogLookup = classification.NeedsCatalogLookup || classification.Intent is "catalog_lookup" or "outfit_recommendation" or "accessory_recommendation" or "product_description" or "product_comparison",
      NeedsClarification = classification.NeedsClarification || classification.Intent == "clarification",
      RetrievalQuery = string.IsNullOrWhiteSpace(classification.RetrievalQuery) ? retrievalQuery : classification.RetrievalQuery,
      SelectionStrategy = selectionStrategy,
      StylistBrief = string.IsNullOrWhiteSpace(classification.StylistBrief) ? BuildStylistBrief(classification, memory) : classification.StylistBrief,
      ReferencedImageHint = NormalizeReferencedImageHint(classification.ReferencedImageHint)
    };
  }

  private static string BuildRetrievalQuery(string message, IntentClassificationDto classification)
  {
    var rawMessage = message?.Trim();
    if (!string.IsNullOrWhiteSpace(rawMessage))
    {
      return rawMessage;
    }

    var softSlotQuery = string.Join(' ', new[]
    {
      classification.ProductType == "phu_kien" ? "phu kien" : "ao dai",
      classification.ColorFamily,
      classification.MaterialKeyword,
      classification.Scenario?.Replace('-', ' ')
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    return string.IsNullOrWhiteSpace(softSlotQuery)
      ? (classification.ProductType == "phu_kien" ? "phu kien" : "ao dai")
      : softSlotQuery;
  }

  private static string BuildStylistBrief(IntentClassificationDto classification, ThreadMemoryStateDto memory)
  {
    var parts = new List<string>();

    if (!string.IsNullOrWhiteSpace(classification.Scenario ?? memory.Scenario))
    {
      parts.Add($"Ưu tiên bối cảnh {(classification.Scenario ?? memory.Scenario)!.Replace('-', ' ')}");
    }

    if (!string.IsNullOrWhiteSpace(classification.ColorFamily ?? memory.ColorFamily))
    {
      parts.Add($"giữ tông {(classification.ColorFamily ?? memory.ColorFamily)}");
    }

    if (!string.IsNullOrWhiteSpace(classification.MaterialKeyword ?? memory.MaterialKeyword))
    {
      parts.Add($"ưu tiên chất liệu {classification.MaterialKeyword ?? memory.MaterialKeyword}");
    }

    if (classification.RequiresPersonImage)
    {
      parts.Add("cần ảnh người mặc trước khi thử đồ");
    }

    return parts.Count == 0 ? "Trả lời như stylist bán hàng giàu kinh nghiệm, ngắn gọn và quyết đoán." : string.Join(". ", parts) + ".";
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

  private static string BuildPrompt(
    string message,
    IReadOnlyList<ChatAttachmentDto> attachments,
    ThreadMemoryStateDto memory,
    string? previousUserMessage,
    string? previousAssistantMessage)
  {
    var builder = new StringBuilder();
    builder.AppendLine("Bạn là bộ phân loại intent cho stylist AI Ao Dai Nha Uyen.");
    builder.AppendLine("Mục tiêu: suy ra ý định hành động tốt nhất từ tin nhắn mới nhất, attachment hiện tại, memory và hội thoại gần đây.");
    builder.AppendLine("Ưu tiên hiểu mục tiêu thật sự của user theo ngữ nghĩa và ngữ cảnh follow-up, không dựa vào khớp cụm từ máy móc.");
    builder.AppendLine("Chỉ dùng các intent: catalog_lookup, outfit_recommendation, accessory_recommendation, product_description, product_comparison, tryon_prepare, tryon_execute, image_style_analysis, clarification, out_of_scope.");
    builder.AppendLine("Định nghĩa intent:");
    builder.AppendLine("- catalog_lookup: user muốn tìm, xem hoặc duyệt sản phẩm phù hợp.");
    builder.AppendLine("- outfit_recommendation: user muốn gợi ý outfit/set phối hoàn chỉnh hoặc phối cùng món đang nói tới.");
    builder.AppendLine("- accessory_recommendation: user chỉ muốn phụ kiện, không phải set hoàn chỉnh.");
    builder.AppendLine("- product_description: user muốn giải thích, nhận xét, mô tả hoặc làm rõ sản phẩm đang được nhắc tới.");
    builder.AppendLine("- product_comparison: user muốn so sánh hoặc chọn giữa nhiều sản phẩm.");
    builder.AppendLine("- tryon_prepare: user muốn thử đồ nhưng còn thiếu điều kiện để thực hiện an toàn.");
    builder.AppendLine("- tryon_execute: user muốn thử đồ và đủ dữ kiện để tiến hành.");
    builder.AppendLine("- image_style_analysis: user muốn nhận xét phong cách từ ảnh mới hoặc ảnh đã có trong memory.");
    builder.AppendLine("- clarification: chưa đủ chắc chắn để chọn hành động an toàn.");
    builder.AppendLine("- out_of_scope: nội dung thuộc đơn hàng, giao hàng, vận chuyển, đổi trả, hoàn tiền hoặc hỗ trợ ngoài stylist.");
    builder.AppendLine("Scenario chỉ được là: giao-vien, le-tet, du-tiec, chup-anh.");
    builder.AppendLine("ColorFamily chỉ được là: blue, pink, red, ivory, black, purple, green, yellow, brown, gold, white.");
    builder.AppendLine("Nếu user nói theo kiểu liệt kê/xem danh sách/xem tất cả/có mẫu nào/có gì phù hợp thì ưu tiên catalog_lookup thay vì clarification.");
    builder.AppendLine("retrievalQuery nên giữ sát câu user nhất có thể; không rewrite sang câu máy móc nếu raw message đã đủ tốt để search catalog.");
    builder.AppendLine("MaterialKeyword chỉ được là: lụa, gấm.");
    builder.AppendLine("ProductType chỉ được là: ao_dai, phu_kien hoặc null.");
    builder.AppendLine("Decision rules:");
    builder.AppendLine("- Suy luận follow-up từ shortlist, selected garment, image catalog, recent messages và previous turns.");
    builder.AppendLine("- Chọn intent hỗ trợ hành động tiếp theo rõ nhất cho user.");
    builder.AppendLine("- Chỉ chọn clarification khi thiếu dữ kiện hoặc intent thật sự mơ hồ.");
    builder.AppendLine("- Không bịa sản phẩm, ảnh, hay ngữ cảnh không có trong input/memory.");
    builder.AppendLine("- requiresPersonImage=true chỉ khi user muốn thử đồ nhưng chưa có ảnh người mặc mới hoặc trong memory.");
    builder.AppendLine("- ReferencedImageHint chỉ được là: first, last, tryon_result, image_1, image_2, image_3 hoặc null.");
    builder.AppendLine("- ProductReferenceScope chỉ được là: none, selected_garment, shortlist_all, shortlist_top_3.");
    builder.AppendLine("- WantsDifferentOptions=true khi user muốn xem hướng khác, mẫu khác hoặc đổi gu; không suy từ keyword máy móc nếu ngữ cảnh không phải đổi lựa chọn.");
    builder.AppendLine("- HasSpecificAccessoryRequest=true khi user hỏi loại phụ kiện cụ thể như túi xách, trâm cài, quạt, guốc, giày, kẹp tóc.");
    builder.AppendLine("- Nếu user đang nói về món vừa chọn, dùng ProductReferenceScope=selected_garment thay vì đoán id cụ thể.");
    builder.AppendLine("Trả về JSON duy nhất, không markdown.");
    builder.AppendLine("JSON fields: intent, scenario, budgetCeiling, colorFamily, materialKeyword, productType, requiresPersonImage, responseMode, needsCatalogLookup, needsClarification, retrievalQuery, selectionStrategy, stylistBrief, referencedImageHint, productReferenceScope, wantsDifferentOptions, hasSpecificAccessoryRequest.");
    builder.AppendLine("Không viết giải thích ngoài JSON.");
    builder.AppendLine();
    builder.AppendLine($"Message: {message}");
    builder.AppendLine($"Attachment kinds: {(attachments.Count == 0 ? "none" : string.Join(", ", attachments.Select(item => item.Kind)))}");
    builder.AppendLine($"Memory scenario: {memory.Scenario ?? "none"}");
    builder.AppendLine($"Memory budget: {(memory.BudgetCeiling?.ToString() ?? "none")}");
    builder.AppendLine($"Memory color: {memory.ColorFamily ?? "none"}");
    builder.AppendLine($"Memory material: {memory.MaterialKeyword ?? "none"}");
    builder.AppendLine($"Memory has person image: {memory.LatestPersonAttachmentId.HasValue}");
    builder.AppendLine($"Memory image catalog: {(memory.ImageCatalog.Count == 0 ? "none" : string.Join(", ", memory.ImageCatalog.Select(e => $"{e.Label} ({e.Kind})")))}");
    builder.AppendLine($"Memory selected garment product id: {(memory.SelectedGarmentProductId?.ToString() ?? "none")}");
    builder.AppendLine($"Memory shortlisted product ids: {(memory.ShortlistedProductIds.Count == 0 ? "none" : string.Join(", ", memory.ShortlistedProductIds))}");
    builder.AppendLine($"User conversation summary: {memory.UserConversationSummary ?? "none"}");
    builder.AppendLine($"Assistant conversation summary: {memory.AssistantConversationSummary ?? "none"}");
    builder.AppendLine($"Recent user messages: {(memory.RecentUserMessages.Count == 0 ? "none" : string.Join(" || ", memory.RecentUserMessages))}");
    builder.AppendLine($"Recent assistant messages: {(memory.RecentAssistantMessages.Count == 0 ? "none" : string.Join(" || ", memory.RecentAssistantMessages))}");
    builder.AppendLine($"Previous user message: {previousUserMessage ?? "none"}");
    builder.AppendLine($"Previous assistant message: {previousAssistantMessage ?? "none"}");
    builder.AppendLine("Ưu tiên conversation summary và recent messages hơn memory summary khi đây là follow-up dài hoặc kéo dài nhiều lượt.");
    return builder.ToString();
  }

  private static string? NormalizeAllowed(string? value, IEnumerable<string> allowed) =>
    allowed.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));

  private static string? NormalizeReferencedImageHint(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    var normalized = value.Trim().ToLowerInvariant();
    if (normalized is "first" or "last" or "tryon_result")
    {
      return normalized;
    }

    if (normalized.StartsWith("image_", StringComparison.Ordinal) && int.TryParse(normalized[6..], out var index) && index > 0)
    {
      return normalized;
    }

    return null;
  }

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

  private static string? DetectProductType(string message)
  {
    var normalized = ChatTextUtils.Normalize(message);
    if (normalized.Contains("phu kien") || normalized.Contains("phụ kiện") || normalized.Contains("khan") || normalized.Contains("trang suc") || normalized.Contains("bong tai") || normalized.Contains("tui xach") || normalized.Contains("tui sach") || normalized.Contains("tram cai") || normalized.Contains("quat") || normalized.Contains("guoc") || normalized.Contains("giay"))
    {
      return "phu_kien";
    }

    if (normalized.Contains("ao dai") || normalized.Contains("áo dài"))
    {
      return "ao_dai";
    }

    return null;
  }

  private static IntentClassificationDto BuildDegradedFallbackClassification(
    string message,
    string normalized,
    ThreadMemoryStateDto memory,
    bool hasImageAttachments)
  {
    var scenario = ExtractScenario(normalized) ?? memory.Scenario;
    var budgetCeiling = ChatTextUtils.TryExtractBudget(message) ?? memory.BudgetCeiling;
    var colorFamily = ExtractColorFamily(normalized) ?? memory.ColorFamily;
    var materialKeyword = ExtractMaterialKeyword(normalized) ?? memory.MaterialKeyword;
    var productType = DetectProductType(message) ?? "ao_dai";

    if (normalized.Contains("giao hang") || normalized.Contains("van chuyen") || normalized.Contains("doi tra") || normalized.Contains("hoan tien"))
    {
      return CreateOutOfScopeIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments);
    }

    if (hasImageAttachments && !IsTryOnIntent(normalized) && !IsCatalogLookupIntent(normalized) && !IsNaturalCatalogNeedIntent(normalized))
    {
      return CreateImageAnalysisIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments) with { ReferencedImageHint = "last" };
    }

    if (IsImageReferenceFollowUp(normalized) && !IsTryOnIntent(normalized))
    {
      return memory.ImageCatalog.Count > 0
        ? CreateImageAnalysisIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments) with { ReferencedImageHint = InferReferencedImageHint(normalized, memory) }
        : CreateClarificationIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments);
    }

    if (IsTryOnIntent(normalized))
    {
      var hasGarment = memory.SelectedGarmentProductId.HasValue || memory.ShortlistedProductIds.Count > 0 || memory.GarmentShortlistedProductIds.Count > 0;
      if (!hasGarment)
      {
        return CreateClarificationIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments);
      }

      return CreateTryOnIntent(scenario, budgetCeiling, colorFamily, materialKeyword, hasImageAttachments, memory) with
      {
        Intent = memory.LatestPersonAttachmentId.HasValue || hasImageAttachments ? "tryon_execute" : "tryon_prepare",
        ReferencedImageHint = InferReferencedImageHint(normalized, memory)
      };
    }

    if (IsCatalogLookupIntent(normalized) || IsNaturalCatalogNeedIntent(normalized))
    {
      return CreateCatalogLookupIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments);
    }

    return CreateClarificationIntent(scenario, budgetCeiling, colorFamily, materialKeyword, productType, hasImageAttachments);
  }

  private static IntentClassificationDto CreateCatalogLookupIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    bool hasImageAttachments) =>
    new(
      "catalog_lookup",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      [],
      false,
      hasImageAttachments);

  private static bool IsCatalogLookupIntent(string normalized) =>
    normalized.Contains("liet ke") ||
    normalized.Contains("danh sach") ||
    normalized.Contains("xem tat ca") ||
    normalized.Contains("xem het") ||
    normalized.Contains("cho xem") ||
    normalized.Contains("co gi") ||
    normalized.Contains("co may mau") ||
    normalized.Contains("tat ca mau") ||
    normalized.Contains("mau nao") ||
    normalized.Contains("co nhung") ||
    normalized.Contains("dang co");

  private static bool IsNaturalCatalogNeedIntent(string normalized) =>
    (normalized.Contains("muon xem") || normalized.Contains("tim") || normalized.Contains("can tim") || normalized.Contains("muon tim")) &&
    (normalized.Contains("ao dai") || normalized.Contains("phu kien") || normalized.Contains("mau") || normalized.Contains("san pham"));


  private static IntentClassificationDto CreateOutOfScopeIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    bool hasImageAttachments) =>
    new(
      "out_of_scope",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      [],
      false,
      hasImageAttachments);

  private static IntentClassificationDto CreateImageAnalysisIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    bool hasImageAttachments) =>
    new(
      "image_style_analysis",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      [],
      false,
      hasImageAttachments);

  private static IntentClassificationDto CreateTryOnIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    bool hasImageAttachments,
    ThreadMemoryStateDto memory) =>
    new(
      "tryon_execute",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      "ao_dai",
      [],
      !memory.LatestPersonAttachmentId.HasValue,
      hasImageAttachments);

  private static IntentClassificationDto CreateProductDescriptionIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    bool hasImageAttachments) =>
    new(
      "product_description",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      [],
      false,
      hasImageAttachments);

  private static IntentClassificationDto CreateComparisonIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    bool hasImageAttachments) =>
    new(
      "product_comparison",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      [],
      false,
      hasImageAttachments);

  private static IntentClassificationDto CreateSetCompletionIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    bool hasImageAttachments) =>
    new(
      "outfit_recommendation",
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      "ao_dai",
      [],
      false,
      hasImageAttachments);

  private static IntentClassificationDto CreateClarificationIntent(
    string? scenario,
    decimal? budgetCeiling,
    string? colorFamily,
    string? materialKeyword,
    string? productType,
    bool hasImageAttachments) =>
    IntentClassificationDto.Clarification(
      scenario,
      budgetCeiling,
      colorFamily,
      materialKeyword,
      productType,
      hasImageAttachments: hasImageAttachments);

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


  private static string? ExtractFirstBalancedJsonObject(string text)
  {
    var trimmed = text.Trim();
    if (trimmed.StartsWith("```", StringComparison.Ordinal))
    {
      var firstLineEnd = trimmed.IndexOf('\n');
      if (firstLineEnd >= 0)
      {
        trimmed = trimmed[(firstLineEnd + 1)..].Trim();
      }

      if (trimmed.EndsWith("```", StringComparison.Ordinal))
      {
        trimmed = trimmed[..^3].Trim();
      }
    }

    var start = trimmed.IndexOf('{');
    if (start < 0)
    {
      return null;
    }

    var depth = 0;
    var inString = false;
    var escaped = false;
    for (var i = start; i < trimmed.Length; i++)
    {
      var current = trimmed[i];
      if (inString)
      {
        if (escaped)
        {
          escaped = false;
        }
        else if (current == '\\')
        {
          escaped = true;
        }
        else if (current == '"')
        {
          inString = false;
        }
        continue;
      }

      if (current == '"')
      {
        inString = true;
        continue;
      }

      if (current == '{')
      {
        depth++;
      }
      else if (current == '}')
      {
        depth--;
        if (depth == 0)
        {
          return trimmed[start..(i + 1)];
        }
      }
    }

    return null;
  }
  private sealed record PlannerOutput(

    string? Intent,
    string? Scenario,
    decimal? BudgetCeiling,
    string? ColorFamily,
    string? MaterialKeyword,
    string? ProductType,
    bool? RequiresPersonImage,
    string? ResponseMode,
    bool? NeedsCatalogLookup,
    bool? NeedsClarification,
    string? RetrievalQuery,
    string? SelectionStrategy,
    string? StylistBrief,
    string? ReferencedImageHint,
    string? ProductReferenceScope,
    bool? WantsDifferentOptions,
    bool? HasSpecificAccessoryRequest);

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


  private static bool IsOutOfScope(string normalized) =>
    normalized.Contains("giao hang") ||
    normalized.Contains("van chuyen") ||
    normalized.Contains("doi tra") ||
    normalized.Contains("hoan tien");

  private static bool IsTryOnIntent(string normalized) =>
    normalized.Contains("thu do") ||
    normalized.Contains("thu ngay") ||
    normalized.Contains("thu luon") ||
    normalized.Contains("uom thu") ||
    normalized.Contains("thu voi anh nay") ||
    normalized.Contains("thu mau nay");

  private static bool IsTryOnImageReferenceIntent(string normalized) =>
    normalized.Contains("thu do anh") ||
    normalized.Contains("thu anh nay") ||
    normalized.Contains("thu anh do") ||
    normalized.Contains("thu voi anh nay") ||
    normalized.Contains("thu voi anh do") ||
    normalized.Contains("thu ket qua thu do");

  private static bool IsImageAnalysisIntent(string normalized) =>
    normalized.Contains("nhin anh") ||
    normalized.Contains("xem anh") ||
    normalized.Contains("nhan xet anh") ||
    normalized.Contains("goi y style tu anh");

  private static bool IsImageReferenceFollowUp(string normalized) =>
    normalized.Contains("anh dau tien") ||
    normalized.Contains("anh cuoi") ||
    normalized.Contains("anh vua tao") ||
    normalized.Contains("anh vua gui") ||
    normalized.Contains("anh thu") ||
    normalized.Contains("anh nay") ||
    normalized.Contains("anh do") ||
    normalized.Contains("cai anh") ||
    normalized.Contains("anh co dep") ||
    normalized.Contains("dep khong") && normalized.Contains("anh");

  private static bool IsProductDescriptionIntent(string normalized) =>
    normalized.Contains("chi tiet") ||
    normalized.Contains("dac tinh") ||
    normalized.Contains("mo ta") ||
    normalized.Contains("noi ro hon") ||
    normalized.Contains("nhan xet");

  private static bool IsComparisonIntent(string normalized) =>
    normalized.Contains("so sanh") ||
    normalized.Contains("khac nhau") ||
    normalized.Contains("nen chon mau nao");

  private static string? InferReferencedImageHint(string normalized, ThreadMemoryStateDto memory)
  {
    if (memory.ImageCatalog.Count == 0)
    {
      return null;
    }

    if (normalized.Contains("anh dau tien") || normalized.Contains("anh 1") || normalized.Contains("anh thu 1"))
    {
      return "first";
    }

    if (normalized.Contains("anh cuoi") || normalized.Contains("anh vua tao") || normalized.Contains("anh vua gui") || normalized.Contains("anh moi"))
    {
      return "last";
    }

    if (normalized.Contains("anh thu do") || normalized.Contains("ket qua thu do"))
    {
      return "tryon_result";
    }

    if (TryExtractImageIndex(normalized, out var imageIndex) && imageIndex > 0)
    {
      return $"image_{imageIndex}";
    }

    return normalized.Contains("anh nay") || normalized.Contains("anh do") || normalized.Contains("cai anh")
      ? "last"
      : null;
  }

  private static bool TryExtractImageIndex(string normalized, out int index)
  {
    index = 0;
    var patterns = new[] { "anh thu ", "anh so ", "anh " };
    foreach (var pattern in patterns)
    {
      var pos = normalized.IndexOf(pattern, StringComparison.Ordinal);
      if (pos < 0)
      {
        continue;
      }

      var afterPattern = normalized[(pos + pattern.Length)..];
      var digits = new string(afterPattern.TakeWhile(char.IsDigit).ToArray());
      if (int.TryParse(digits, out var number) && number > 0)
      {
        index = number;
        return true;
      }
    }

    return false;
  }

  private static bool IsSetCompletionIntent(string normalized, ThreadMemoryStateDto memory)
  {
    var hasGarmentContext = memory.SelectedGarmentProductId.HasValue || memory.ShortlistedProductIds.Count > 0 || memory.GarmentShortlistedProductIds.Count > 0;
    if (!hasGarmentContext)
    {
      return false;
    }

    return normalized.Contains("di cap") ||
      normalized.Contains("phoi sao") ||
      normalized.Contains("phoi cung") ||
      normalized.Contains("goi y set") ||
      normalized.Contains("mix cung") ||
      normalized.Contains("set hoan chinh") ||
      normalized.Contains("nen di cung");
  }

}
