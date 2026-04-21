using System.Text.Json;
using AoDaiNhaUyen.Application.DTOs;
using AoDaiNhaUyen.Application.Interfaces.Services;
using AoDaiNhaUyen.Domain.Entities;

namespace AoDaiNhaUyen.Infrastructure.Services;

public sealed class ThreadMemoryService : IThreadMemoryService
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public ThreadMemoryStateDto Read(ChatThreadMemory? memory)
  {
    if (memory is null)
    {
      return new ThreadMemoryStateDto();
    }

    var facts = Deserialize<StoredFacts>(memory.FactsJsonb) ?? new StoredFacts();
    var refs = Deserialize<StoredRefs>(memory.ResolvedRefsJsonb) ?? new StoredRefs();

    return new ThreadMemoryStateDto
    {
      Scenario = facts.Scenario,
      BudgetCeiling = facts.BudgetCeiling,
      ColorFamily = facts.ColorFamily,
      MaterialKeyword = facts.MaterialKeyword,
      ConversationStage = facts.ConversationStage,
      LastUserRequestType = facts.LastUserRequestType,
      ShortlistedProductIds = refs.LastShortlistProductIds ?? [],
      GarmentShortlistedProductIds = refs.LastGarmentShortlistProductIds ?? [],
      AccessoryShortlistedProductIds = refs.LastAccessoryShortlistProductIds ?? [],
      ShownProductIds = refs.ShownProductIds ?? [],
      ShownGarmentProductIds = refs.ShownGarmentProductIds ?? [],
      ShownAccessoryProductIds = refs.ShownAccessoryProductIds ?? [],
      ShownOutfitSignatures = refs.ShownOutfitSignatures ?? [],
      RejectedProductIds = refs.RejectedProductIds ?? [],
      LikedProductIds = refs.LikedProductIds ?? [],
      LastRecommendationStrategy = facts.LastRecommendationStrategy,
      SelectedGarmentProductId = facts.SelectedGarmentProductId,
      SelectedAccessoryProductIds = facts.SelectedAccessoryProductIds ?? [],
      LatestPersonAttachmentId = facts.LatestPersonAttachmentId,
      LatestTryOnResultAttachmentId = facts.LatestTryOnResultAttachmentId,
      LatestTryOnResultMessageId = facts.LatestTryOnResultMessageId,
      PendingTryOnRequirements = facts.PendingTryOnRequirements ?? [],
      RecentUserMessages = facts.RecentUserMessages ?? [],
      RecentAssistantMessages = facts.RecentAssistantMessages ?? [],
      UserConversationSummary = facts.UserConversationSummary,
      AssistantConversationSummary = facts.AssistantConversationSummary
    };
  }

  public void ApplyUserTurn(
    ThreadMemoryStateDto memory,
    IReadOnlyList<ChatAttachment> attachments)
  {
    var latestPersonImage = attachments.LastOrDefault(attachment => attachment.Kind == "user_image");
    if (latestPersonImage is not null)
    {
      memory.LatestPersonAttachmentId = latestPersonImage.Id;
    }
  }

  public void ApplyUserConversationTurn(ThreadMemoryStateDto memory, string userMessage)
  {
    if (string.IsNullOrWhiteSpace(userMessage))
    {
      return;
    }

    var normalizedMessage = ChatTextUtils.Normalize(userMessage);
    memory.LastUserRequestType = DetectUserRequestType(normalizedMessage);

    if (normalizedMessage.Contains("khong thich")
        || normalizedMessage.Contains("ko thich")
        || normalizedMessage.Contains("k thich")
        || normalizedMessage.Contains("khong ung")
        || normalizedMessage.Contains("ko ung")
        || normalizedMessage.Contains("khong hop")
        || normalizedMessage.Contains("ko hop"))
    {
      memory.RejectedProductIds = memory.RejectedProductIds
        .Concat(memory.GarmentShortlistedProductIds)
        .Concat(memory.AccessoryShortlistedProductIds)
        .Distinct()
        .ToList();
      memory.ConversationStage = "refine";
    }
    else if (normalizedMessage.Contains("thich") || normalizedMessage.Contains("ung") || normalizedMessage.Contains("chon mau nay") || normalizedMessage.Contains("lay mau nay"))
    {
      memory.LikedProductIds = memory.LikedProductIds
        .Concat(memory.GarmentShortlistedProductIds)
        .Concat(memory.AccessoryShortlistedProductIds)
        .Distinct()
        .ToList();
      memory.ConversationStage = "decide";
    }
    else if (memory.LastUserRequestType == "alternative_request")
    {
      memory.ConversationStage = "refine";
    }

    memory.RecentUserMessages.Add(userMessage.Trim());
    if (memory.RecentUserMessages.Count > 10)
    {
      memory.UserConversationSummary = AppendSummary(memory.UserConversationSummary, memory.RecentUserMessages.Take(10).ToList());
      memory.RecentUserMessages = memory.RecentUserMessages.Skip(10).ToList();
    }
  }

  public void ApplyAssistantTurn(
    ThreadMemoryStateDto memory,
    IntentClassificationDto classification,
    ChatStructuredPayloadDto? structuredPayload,
    long? tryOnResultAttachmentId,
    long? tryOnResultMessageId,
    string? assistantMessage = null)
  {
    if (!string.IsNullOrWhiteSpace(classification.Scenario))
    {
      memory.Scenario = classification.Scenario;
    }

    if (classification.BudgetCeiling.HasValue)
    {
      memory.BudgetCeiling = classification.BudgetCeiling.Value;
    }

    if (!string.IsNullOrWhiteSpace(classification.ColorFamily))
    {
      memory.ColorFamily = classification.ColorFamily;
    }

    if (!string.IsNullOrWhiteSpace(classification.MaterialKeyword))
    {
      memory.MaterialKeyword = classification.MaterialKeyword;
    }

    if (structuredPayload is not null)
    {
      var garmentProducts = (structuredPayload.GarmentProducts ?? [])
        .Where(product => string.Equals(product.ProductType, "ao_dai", StringComparison.OrdinalIgnoreCase))
        .ToList();
      var accessoryProducts = (structuredPayload.AccessoryProducts ?? [])
        .Where(product => string.Equals(product.ProductType, "phu_kien", StringComparison.OrdinalIgnoreCase))
        .ToList();

      if (garmentProducts.Count == 0 && accessoryProducts.Count == 0 && structuredPayload.Products.Count > 0)
      {
        garmentProducts = structuredPayload.Products
          .Where(product => string.Equals(product.ProductType, "ao_dai", StringComparison.OrdinalIgnoreCase))
          .ToList();
        accessoryProducts = structuredPayload.Products
          .Where(product => string.Equals(product.ProductType, "phu_kien", StringComparison.OrdinalIgnoreCase))
          .ToList();
      }

      if (garmentProducts.Count > 0)
      {
        memory.GarmentShortlistedProductIds = garmentProducts.Select(product => product.ProductId).Distinct().ToList();
        memory.ShortlistedProductIds = [.. memory.GarmentShortlistedProductIds];
        memory.ShownGarmentProductIds = memory.ShownGarmentProductIds
          .Concat(memory.GarmentShortlistedProductIds)
          .Distinct()
          .ToList();
      }
      else if (structuredPayload.Products.Count > 0 && accessoryProducts.Count == 0)
      {
        memory.ShortlistedProductIds = structuredPayload.Products.Select(product => product.ProductId).Distinct().ToList();
      }

      if (accessoryProducts.Count > 0)
      {
        memory.AccessoryShortlistedProductIds = accessoryProducts.Select(product => product.ProductId).Distinct().ToList();
        memory.ShownAccessoryProductIds = memory.ShownAccessoryProductIds
          .Concat(memory.AccessoryShortlistedProductIds)
          .Distinct()
          .ToList();
      }

      var hadShownProducts = memory.ShownProductIds.Count > 0;
      var shownIds = structuredPayload.Products
        .Concat(structuredPayload.GarmentProducts ?? [])
        .Concat(structuredPayload.AccessoryProducts ?? [])
        .Select(product => product.ProductId)
        .Distinct();
      memory.ShownProductIds = memory.ShownProductIds
        .Concat(shownIds)
        .Distinct()
        .ToList();

      memory.SelectedGarmentProductId = structuredPayload.SelectedGarmentProductId ?? memory.SelectedGarmentProductId;
      memory.SelectedAccessoryProductIds = structuredPayload.SelectedAccessoryProductIds.Distinct().ToList();
      memory.PendingTryOnRequirements = structuredPayload.PendingTryOnRequirements.Distinct().ToList();
      memory.LastRecommendationStrategy = classification.Intent is "outfit_recommendation" or "catalog_lookup" or "image_style_analysis"
        ? (hadShownProducts ? "novelty_first" : "relevance_first")
        : memory.LastRecommendationStrategy;

      if (classification.Intent is "outfit_recommendation" or "catalog_lookup" or "image_style_analysis")
      {
        memory.LastUserRequestType ??= hadShownProducts ? "alternative_request" : "discovery_request";
      }
      memory.ConversationStage = classification.Intent switch
      {
        "product_comparison" => "compare",
        "tryon_prepare" or "tryon_execute" => "decide",
        "outfit_recommendation" or "catalog_lookup" or "accessory_recommendation" => memory.LastRecommendationStrategy == "novelty_first" ? "refine" : "discovery",
        _ => memory.ConversationStage
      };

      if ((structuredPayload.GarmentProducts?.Count > 0 || structuredPayload.AccessoryProducts?.Count > 0) && structuredPayload.SelectedGarmentProductId.HasValue)
      {
        var signature = BuildOutfitSignature(structuredPayload.SelectedGarmentProductId.Value, structuredPayload.SelectedAccessoryProductIds);
        if (!string.IsNullOrWhiteSpace(signature))
        {
          memory.ShownOutfitSignatures = memory.ShownOutfitSignatures
            .Concat([signature])
            .Distinct()
            .ToList();
        }
      }
    }

    if (tryOnResultAttachmentId.HasValue)
    {
      memory.LatestTryOnResultAttachmentId = tryOnResultAttachmentId.Value;
    }

    if (tryOnResultMessageId.HasValue)
    {
      memory.LatestTryOnResultMessageId = tryOnResultMessageId.Value;
    }

    if (!string.IsNullOrWhiteSpace(assistantMessage))
    {
      memory.RecentAssistantMessages.Add(assistantMessage.Trim());
      if (memory.RecentAssistantMessages.Count > 10)
      {
        memory.AssistantConversationSummary = AppendSummary(memory.AssistantConversationSummary, memory.RecentAssistantMessages.Take(10).ToList());
        memory.RecentAssistantMessages = memory.RecentAssistantMessages.Skip(10).ToList();
      }
    }
  }

  public void Persist(
    ChatThread thread,
    ThreadMemoryStateDto memory,
    long? lastMessageId)
  {
    thread.Memory ??= new ChatThreadMemory
    {
      ThreadId = thread.Id
    };

    thread.Memory.Summary = BuildSummary(memory);
    thread.Memory.FactsJsonb = JsonSerializer.Serialize(new StoredFacts
    {
      Scenario = memory.Scenario,
      BudgetCeiling = memory.BudgetCeiling,
      ColorFamily = memory.ColorFamily,
      MaterialKeyword = memory.MaterialKeyword,
      ConversationStage = memory.ConversationStage,
      LastUserRequestType = memory.LastUserRequestType,
      LastRecommendationStrategy = memory.LastRecommendationStrategy,
      SelectedGarmentProductId = memory.SelectedGarmentProductId,
      SelectedAccessoryProductIds = memory.SelectedAccessoryProductIds,
      LatestPersonAttachmentId = memory.LatestPersonAttachmentId,
      LatestTryOnResultAttachmentId = memory.LatestTryOnResultAttachmentId,
      LatestTryOnResultMessageId = memory.LatestTryOnResultMessageId,
      PendingTryOnRequirements = memory.PendingTryOnRequirements,
      RecentUserMessages = memory.RecentUserMessages,
      RecentAssistantMessages = memory.RecentAssistantMessages,
      UserConversationSummary = memory.UserConversationSummary,
      AssistantConversationSummary = memory.AssistantConversationSummary
    }, JsonOptions);
    thread.Memory.ResolvedRefsJsonb = JsonSerializer.Serialize(new StoredRefs
    {
      LastShortlistProductIds = memory.ShortlistedProductIds,
      LastGarmentShortlistProductIds = memory.GarmentShortlistedProductIds,
      LastAccessoryShortlistProductIds = memory.AccessoryShortlistedProductIds,
      ShownProductIds = memory.ShownProductIds,
      ShownGarmentProductIds = memory.ShownGarmentProductIds,
      ShownAccessoryProductIds = memory.ShownAccessoryProductIds,
      ShownOutfitSignatures = memory.ShownOutfitSignatures,
      RejectedProductIds = memory.RejectedProductIds,
      LikedProductIds = memory.LikedProductIds
    }, JsonOptions);
    thread.Memory.LastMessageId = lastMessageId;
    thread.Memory.UpdatedAt = DateTime.UtcNow;
  }

  private static string BuildSummary(ThreadMemoryStateDto memory)
  {
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(memory.Scenario))
    {
      parts.Add($"dịp {memory.Scenario}");
    }

    if (memory.BudgetCeiling.HasValue)
    {
      parts.Add($"ngân sách <= {memory.BudgetCeiling.Value:N0}đ");
    }

    if (!string.IsNullOrWhiteSpace(memory.ColorFamily))
    {
      parts.Add($"màu {memory.ColorFamily}");
    }

    if (!string.IsNullOrWhiteSpace(memory.MaterialKeyword))
    {
      parts.Add($"chất liệu {memory.MaterialKeyword}");
    }

    return parts.Count == 0
      ? "Chưa có ngữ cảnh ổn định."
      : string.Join("; ", parts);
  }

  private static string AppendSummary(string? existingSummary, IReadOnlyList<string> messages)
  {
    var summary = string.Join("\n", messages.Select((message, index) => $"{index + 1}. {message}"));
    return string.IsNullOrWhiteSpace(existingSummary)
      ? summary
      : string.Concat(existingSummary, "\n", summary);
  }

  private static string? BuildOutfitSignature(long selectedGarmentProductId, IReadOnlyList<long> selectedAccessoryProductIds)
  {
    var accessoryIds = selectedAccessoryProductIds
      .Distinct()
      .OrderBy(id => id)
      .ToArray();
    return $"{selectedGarmentProductId}:{string.Join('-', accessoryIds)}";
  }

  private static T? Deserialize<T>(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return default;
    }

    try
    {
      return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
    catch (JsonException)
    {
      return default;
    }
  }

  private static string DetectUserRequestType(string normalizedMessage)
  {
    if (normalizedMessage.Contains("khac") || normalizedMessage.Contains("them mau nua") || normalizedMessage.Contains("them vai mau nua"))
    {
      return "alternative_request";
    }

    if (normalizedMessage.Contains("so sanh") || normalizedMessage.Contains("chon mau nao"))
    {
      return "comparison_request";
    }

    if (normalizedMessage.Contains("thu do") || normalizedMessage.Contains("thu ngay") || normalizedMessage.Contains("thu luon"))
    {
      return "tryon_request";
    }

    if (normalizedMessage.Contains("thich") || normalizedMessage.Contains("ung") || normalizedMessage.Contains("chon mau nay") || normalizedMessage.Contains("lay mau nay"))
    {
      return "selection_feedback";
    }

    return "discovery_request";
  }

  private sealed class StoredFacts
  {
    public string? Scenario { get; set; }
    public decimal? BudgetCeiling { get; set; }
    public string? ColorFamily { get; set; }
    public string? MaterialKeyword { get; set; }
    public string? ConversationStage { get; set; }
    public string? LastUserRequestType { get; set; }
    public string? LastRecommendationStrategy { get; set; }
    public long? SelectedGarmentProductId { get; set; }
    public List<long>? SelectedAccessoryProductIds { get; set; }
    public long? LatestPersonAttachmentId { get; set; }
    public long? LatestTryOnResultAttachmentId { get; set; }
    public long? LatestTryOnResultMessageId { get; set; }
    public List<string>? PendingTryOnRequirements { get; set; }
    public List<string>? RecentUserMessages { get; set; }
    public List<string>? RecentAssistantMessages { get; set; }
    public string? UserConversationSummary { get; set; }
    public string? AssistantConversationSummary { get; set; }
  }

  private sealed class StoredRefs
  {
    public List<long>? LastShortlistProductIds { get; set; }
    public List<long>? LastGarmentShortlistProductIds { get; set; }
    public List<long>? LastAccessoryShortlistProductIds { get; set; }
    public List<long>? ShownProductIds { get; set; }
    public List<long>? ShownGarmentProductIds { get; set; }
    public List<long>? ShownAccessoryProductIds { get; set; }
    public List<string>? ShownOutfitSignatures { get; set; }
    public List<long>? RejectedProductIds { get; set; }
    public List<long>? LikedProductIds { get; set; }
  }
}
